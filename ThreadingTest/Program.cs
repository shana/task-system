using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace ThreadingTest
{
    interface IProcess<T> : ITask<T>
    {
        event Action<string> OnErrorData;
        int ProcessId { get; }
        StreamWriter StandardInput { get; }
    }

    class ProcessTask<T> : ProcessTask<T, T>
    {
        public ProcessTask(CancellationToken token, IOutputProcessor<T, T> outputProcessor)
            : base(token, outputProcessor)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResults">The type of the results. If it's a List<> or similar, then specify the full List<> type here and the inner type of the List in <typeparam name="TResult"/>
    /// <typeparam name="TResult">If <typeparam name="TResult"/> is a list or similar, then specify its inner type here</typeparam>
    class ProcessTask<TResults, TResult> : TaskBase<TResults, TResult>, IProcess<TResults>
    {
        private IOutputProcessor<TResults, TResult> outputProcessor;
        private Process process;

        private List<string> errors;
        private StreamWriter input;

        public event Action<string> OnErrorData;

        public ProcessTask(CancellationToken token, IOutputProcessor<TResults, TResult> outputProcessor = null)
            : base(token)
        {
            Guard.ArgumentNotNull(token, "token");

            this.outputProcessor = outputProcessor;
            errors = new List<string>();
        }

        public void Configure(ProcessStartInfo psi, IOutputProcessor<TResults, TResult> processor = null)
        {
            Guard.ArgumentNotNull(psi, "psi");

            outputProcessor = processor ?? outputProcessor;
            outputProcessor.OnEntry += x => RaiseOnData(x);
            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            Task = new Task<TResults>(Run, Token, TaskCreationOptions.None);
        }

        private TResults Run()
        {
            WaitForDependentTask();

            Console.WriteLine(String.Format("Executing id:{0} thread:{1}", Task.Id, Thread.CurrentThread.ManagedThreadId));

            process.OutputDataReceived += (s, e) =>
            {
                //logger.Trace("OutputData \"" + (e.Data == null ? "'null'" : e.Data) + "\" exited:" + process.HasExited);

                string encodedData = null;
                if (e.Data != null)
                {
                    encodedData = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data));
                }
                outputProcessor.LineReceived(encodedData);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    //logger.Trace("ErrorData \"" + (e.Data == null ? "'null'" : e.Data) + "\" exited:" + process.HasExited);
                }

                string encodedData = null;
                if (e.Data != null)
                {
                    encodedData = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data));
                    errors.Add(encodedData);
                }
            };

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error code " + ex.NativeErrorCode);
                if (ex.NativeErrorCode == 2)
                {
                    sb.AppendLine("The system cannot find the file specified.");
                }
                foreach (string env in process.StartInfo.EnvironmentVariables.Keys)
                {
                    sb.AppendFormat("{0}:{1}", env, process.StartInfo.EnvironmentVariables[env]);
                    sb.AppendLine();
                }
                OnErrorData?.Invoke(String.Format("{0} {1}", ex.Message, sb.ToString()));
                RaiseOnEnd();
                throw;
            }
            if (process.StartInfo.RedirectStandardOutput)
                process.BeginOutputReadLine();
            if (process.StartInfo.RedirectStandardError)
                process.BeginErrorReadLine();
            if (process.StartInfo.RedirectStandardInput)
                input = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false));

            RaiseOnStart();

            while (!WaitForExit(500))
            {
                if (Token.IsCancellationRequested)
                {
                    if (!process.HasExited)
                        process.Kill();
                    process.Close();
                    RaiseOnEnd();
                    Token.ThrowIfCancellationRequested();
                }
            }

            if (process.ExitCode != 0)
            {
                Errors = String.Join(Environment.NewLine, errors.ToArray());
                RaiseOnEnd();
                throw new ProcessException(this);
            }
            RaiseOnEnd();
            return outputProcessor.Result;
        }

        private bool WaitForExit(int milliseconds)
        {
            //logger.Debug("WaitForExit - time: {0}ms", milliseconds);

            // Workaround for a bug in which some data may still be processed AFTER this method returns true, thus losing the data.
            // http://connect.microsoft.com/VisualStudio/feedback/details/272125/waitforexit-and-waitforexit-int32-provide-different-and-undocumented-implementations
            bool waitSucceeded = process.WaitForExit(milliseconds);
            if (waitSucceeded)
            {
                process.WaitForExit();
            }
            return waitSucceeded;
        }

        public int ProcessId { get { return process.Id; } }
        public override bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && process.ExitCode == 0; }}
        public StreamWriter StandardInput { get { return input; } }
    }

    class ProcessManager
    {
        public CancellationToken Token { get; }

        public ProcessManager(CancellationToken token)
        {
            this.Token = token;
        }

        public ProcessStartInfo Configure(string executable, string arguments, bool withInput = false)
        {
            var psi = new ProcessStartInfo(executable, arguments)
            {
                CreateNoWindow = true,
                RedirectStandardInput = withInput,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false
            };
            return psi;
        }
    }

    class GitConfigListTask : ProcessTask<List<KeyValuePair<string, string>>, KeyValuePair<string, string>>
    {
        private readonly static ConfigOutputProcessor defaultProcessor = new ConfigOutputProcessor();
        public GitConfigListTask(ProcessManager processManager, ConfigOutputProcessor processor = null) : base(processManager.Token, processor ?? defaultProcessor)
        {
            Guard.ArgumentNotNull(processManager, "processManager");

            this.Name = "git config";
            var psi = processManager.Configure("git", "confg -l --show-origin");
            Configure(psi);
        }

        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }

    class GitConfigGetTask : ProcessTask<List<KeyValuePair<string, string>>, KeyValuePair<string, string>>
    {
        private readonly static ConfigOutputProcessor defaultProcessor = new ConfigOutputProcessor();
        public GitConfigGetTask(ProcessManager processManager, string key, ConfigOutputProcessor processor = null) : base(processManager.Token, processor ?? defaultProcessor)
        {
            Guard.ArgumentNotNull(processManager, "processManager");

            this.Name = "git config";
            var psi = processManager.Configure("git", String.Format("config --get {0}", key));
            Configure(psi);
        }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tasks = new Dictionary<GitConfigListTask, CancellationTokenSource>();
            var cts = new CancellationTokenSource();
            var processManager = new ProcessManager(cts.Token);

            for (int i = 0; i < 10; i++)
            {
                var outputProcessor = new ConfigOutputProcessor();
                var task = new GitConfigListTask(processManager, outputProcessor);
                tasks.Add(task, cts);
            }

            Run(tasks, cts);
        }

        private static void Run(Dictionary<GitConfigListTask, CancellationTokenSource> tasks, CancellationTokenSource cts)
        {
            var syncContext = new ThreadSynchronizationContext(cts.Token);
            var taskManager = new TaskManager(new SynchronizationContextTaskScheduler(syncContext), cts);

            ITask task = null;

            foreach (var t in tasks)
            {

                task = t.Key;
                task.OnStart += tt => Console.WriteLine(String.Format("Executing id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));
                task.OnEnd += tt => Console.WriteLine(String.Format("Finished id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));

                Console.WriteLine("Scheduling task {0}", task.Task.Id);
                taskManager.Schedule(task);

                var other = new ActionTask<bool, List<KeyValuePair<string, string>>>(cts.Token, d =>
                {
                    Console.WriteLine("Results! {0}", d.Count);
                    return true;
                }, task as GitConfigListTask)
                { Name = "Other", Affinity = TaskAffinity.Concurrent };
                other.OnStart += tt => Console.WriteLine(String.Format("Executing id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));
                other.OnEnd += tt => Console.WriteLine(String.Format("Finished id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));

                Console.WriteLine("Scheduling other {0}", other.Task.Id);
                taskManager.Schedule(other);

                var final = new ActionTask<bool, bool>(cts.Token, d =>
                {
                    return d;
                }, other)
                { Name = "Final", Affinity = TaskAffinity.UI };
                Console.WriteLine("Scheduling final {0}", final.Task.Id);
                final.OnStart += tt => Console.WriteLine(String.Format("Executing id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));
                final.OnEnd += tt => Console.WriteLine(String.Format("Finished id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));

                taskManager.Schedule(final);
            }

            Console.WriteLine("Done");

            Console.Read();
        }
    }

    class ThreadSynchronizationContext : SynchronizationContext
    {
        private readonly CancellationToken token;
        private readonly ConcurrentQueue<PostData> queue = new ConcurrentQueue<PostData>();
        private readonly ConcurrentQueue<PostData> priorityQueue = new ConcurrentQueue<PostData>();
        private readonly JobSignal jobSignal = new JobSignal();
        private long jobId;
        private readonly Task task;
        private int threadId;

        public ThreadSynchronizationContext(CancellationToken token)
        {
            this.token = token;
            task = new Task(Start, token, TaskCreationOptions.LongRunning);
            task.Start();
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            queue.Enqueue(new PostData { Callback = d, State = state });
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            if (Thread.CurrentThread.ManagedThreadId == threadId)
            {
                d(state);
            }
            else
            {
                var id = Interlocked.Increment(ref jobId);
                priorityQueue.Enqueue(new PostData { Id = id, Callback = d, State = state });
                Wait(id);
            }
        }

        private void Wait(long id)
        {
            jobSignal.Wait(id, token);
        }

        private void Start()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
            var lastTime = DateTime.Now.Ticks;
            var wait = new ManualResetEventSlim(false);
            var ticksPerFrame = TimeSpan.TicksPerMillisecond * 10;
            while (!token.IsCancellationRequested)
            {
                var current = DateTime.Now.Ticks;
                var elapsed = current - lastTime;
                Pump();
                lastTime = DateTime.Now.Ticks;
                long waitTime = (current + ticksPerFrame - lastTime) / TimeSpan.TicksPerMillisecond;
                if (waitTime > 0 && waitTime < int.MaxValue)
                    wait.Wait((int)waitTime, token);
            }
        }

        public void Pump()
        {
            PostData data;
            if (priorityQueue.TryDequeue(out data))
            {
                data.Run();
            }
            if (queue.TryDequeue(out data))
            {
                data.Run();
            }
        }
        struct PostData
        {
            public long Id;
            public SendOrPostCallback Callback;
            public object State;
            public void Run()
            {
                Callback(State);
            }
        }

        class JobSignal : ManualResetEventSlim
        {
            private readonly HashSet<long> signaledIds = new HashSet<long>();

            public void Set(long id)
            {
                try
                {
                    signaledIds.Add(id);
                }
                catch { } // it's already on the list
                Set();
                Reset();
            }

            public bool Wait(long id, CancellationToken token)
            {
                bool signaled = false;
                do
                {

                    signaled = signaledIds.Contains(id);
                    if (signaled)
                        break;
                    Wait(token);
                }
                while (!token.IsCancellationRequested && !signaled);
                return signaled;
            }
        }
    }

    class TaskManager
    {
        private readonly TaskScheduler uiScheduler;
        private readonly CancellationTokenSource cts;
        private readonly ConcurrentExclusiveInterleave manager = new ConcurrentExclusiveInterleave();

        public TaskManager(TaskScheduler uiScheduler, CancellationTokenSource cts)
        {
            this.uiScheduler = uiScheduler;
            this.cts = cts;
        }

        public void Schedule(ITask task)
        {
            switch (task.Affinity)
            {
                case TaskAffinity.Exclusive:
                    ScheduleExclusive(task);
                    break;
                case TaskAffinity.UI:
                    ScheduleUI(task);
                    break;
                case TaskAffinity.Concurrent:
                default:
                    ScheduleConcurrent(task);
                    break;
            }
        }

        public void ScheduleUI(ITask task)
        {
            task.Task.ContinueWith(tt =>
                {
                    Console.WriteLine(String.Format("Exception ui! thread: {0} {1} {2}", Thread.CurrentThread.ManagedThreadId, tt.Id, tt.Exception.InnerException));
                },
                cts.Token, 
                TaskContinuationOptions.OnlyOnFaulted,uiScheduler
            );
            task.Start(uiScheduler);
        }

        public void ScheduleExclusive(ITask task)
        {
            task.Task.ContinueWith(tt =>
                {
                    Console.WriteLine(String.Format("Exception exclusive! thread: {0} {1} {2}", Thread.CurrentThread.ManagedThreadId, tt.Id, tt.Exception.InnerException));
                },
                cts.Token,
                TaskContinuationOptions.OnlyOnFaulted, uiScheduler
            );
            task.Start(manager.ExclusiveTaskScheduler);
        }

        public void ScheduleConcurrent(ITask task)
        {
            task.Task.ContinueWith(tt =>
                {
                    Console.WriteLine(String.Format("Exception concurrent! thread: {0} {1} {2}", Thread.CurrentThread.ManagedThreadId, tt.Id, tt.Exception.InnerException));
                },
                cts.Token,
                TaskContinuationOptions.OnlyOnFaulted, uiScheduler
            );
            task.Start(manager.ConcurrentTaskScheduler);
        }
    }
}
