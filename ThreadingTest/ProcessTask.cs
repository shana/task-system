using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface IProcess
    {
        void Configure(Process existingProcess);
        void Configure(ProcessStartInfo psi);
        event Action<string> OnErrorData;
        StreamWriter StandardInput { get; }
        int ProcessId { get; }
        string ProcessName { get; }
        string ProcessArguments { get; }
    }

    interface IProcessTask<T> : ITask<T>, IProcess
    {
        void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor);
    }

    interface IProcessTask<TResults, TResultEntry> : ITask<TResults, TResultEntry>, IProcess
    {
        void Configure(ProcessStartInfo psi, IOutputProcessor<TResults, TResultEntry> processor);
    }

    static class ProcessTaskExtensions
    {
        public static T Configure<T>(this T task, IProcessManager processManager)
            where T : IProcess
        {
            return processManager.Configure(task);
        }
    }

    class ProcessTask<T> : ProcessTask<T, T>
    {
        private IOutputProcessor<T> outputProcessor;

        public ProcessTask(CancellationToken token, IOutputProcessor<T> outputProcessor = null)
            : base(token)
        {
            this.outputProcessor = outputProcessor;
        }

        public override void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor)
        {
            Guard.ArgumentNotNull(psi, "psi");
            Guard.ArgumentNotNull(processor, "processor");

            outputProcessor = processor ?? outputProcessor;
            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            base.Configure(psi, outputProcessor);
        }

        protected override void ConfigureOutputProcessor()
        {
            if (outputProcessor == null && (typeof(T) != typeof(string)))
            {
                throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
            }
            outputProcessor.OnEntry += x => RaiseOnData(x);
        }
    }

    class ProcessTaskWithListOutput<T> : ProcessTask<List<T>, T>
    {
        public ProcessTaskWithListOutput(CancellationToken token, IOutputProcessor<List<T>, T> outputProcessor = null)
            : base(token, outputProcessor)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResults">The type of the results. If it's a List<> or similar, then specify the full List<> type here and the inner type of the List in <typeparam name="TResult"/>
    /// <typeparam name="TResult">If <typeparam name="TResult"/> is a list or similar, then specify its inner type here</typeparam>
    class ProcessTask<TResults, TResult> : TaskBase<TResults, TResult>, IProcessTask<TResults, TResult>
    {
        private readonly List<string> errors = new List<string>();

        private IOutputProcessor<TResults, TResult> outputProcessor;
        private StreamWriter input;
        public event Action<string> OnErrorData;

        public ProcessTask(CancellationToken token, IOutputProcessor<TResults, TResult> outputProcessor = null)
            : base(token)
        {
            Guard.ArgumentNotNull(token, "token");

            this.outputProcessor = outputProcessor;
        }

        public void Configure(ProcessStartInfo psi)
        {
            Guard.ArgumentNotNull(psi, "psi");

            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            Task = new Task<TResults>(Run, Token, TaskCreationOptions.None);
        }

        public void Configure(ProcessStartInfo psi, IOutputProcessor<TResults, TResult> processor)
        {
            Guard.ArgumentNotNull(psi, "psi");
            Guard.ArgumentNotNull(processor, "processor");

            outputProcessor = processor ?? outputProcessor;
            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            Task = new Task<TResults>(Run, Token, TaskCreationOptions.None);
        }

        public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<TResults> processor)
        {
            Task = new Task<TResults>(Run, Token, TaskCreationOptions.None);
        }

        public void Configure(Process existingProcess)
        {
            Guard.ArgumentNotNull(existingProcess, "existingProcess");

            ConfigureOutputProcessor();
            Process = existingProcess;
            Task = new Task<TResults>(Run, Token, TaskCreationOptions.None);
        }

        protected virtual void ConfigureOutputProcessor()
        {
            if (outputProcessor == null && (typeof(TResults) != typeof(string)))
            {
                throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
            }
            outputProcessor.OnEntry += x => RaiseOnData(x);
        }

        private TResults Run()
        {
            WaitForDependentTask();

            Console.WriteLine(String.Format("Executing id:{0} thread:{1}", Task.Id, Thread.CurrentThread.ManagedThreadId));

            if (Process.StartInfo.RedirectStandardOutput)
            {
                Process.OutputDataReceived += (s, e) =>
                {
                    //logger.Trace("OutputData \"" + (e.Data == null ? "'null'" : e.Data) + "\" exited:" + process.HasExited);

                    string encodedData = null;
                    if (e.Data != null)
                    {
                        encodedData = Encoding.UTF8.GetString(Encoding.Default.GetBytes(e.Data));
                    }
                    outputProcessor.LineReceived(encodedData);
                };
            }

            if (Process.StartInfo.RedirectStandardError)
            {
                Process.ErrorDataReceived += (s, e) =>
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
            }

            try
            {
                Process.Start();
            }
            catch (Win32Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error code " + ex.NativeErrorCode);
                if (ex.NativeErrorCode == 2)
                {
                    sb.AppendLine("The system cannot find the file specified.");
                }
                foreach (string env in Process.StartInfo.EnvironmentVariables.Keys)
                {
                    sb.AppendFormat("{0}:{1}", env, Process.StartInfo.EnvironmentVariables[env]);
                    sb.AppendLine();
                }
                OnErrorData?.Invoke(String.Format("{0} {1}", ex.Message, sb.ToString()));
                RaiseOnEnd();
                throw;
            }
            if (Process.StartInfo.RedirectStandardOutput)
                Process.BeginOutputReadLine();
            if (Process.StartInfo.RedirectStandardError)
                Process.BeginErrorReadLine();
            if (Process.StartInfo.RedirectStandardInput)
                input = new StreamWriter(Process.StandardInput.BaseStream, new UTF8Encoding(false));

            RaiseOnStart();

            if (Process.StartInfo.CreateNoWindow)
            {
                while (!WaitForExit(500))
                {
                    if (Token.IsCancellationRequested)
                    {
                        if (!Process.HasExited)
                            Process.Kill();
                        Process.Close();
                        RaiseOnEnd();
                        Token.ThrowIfCancellationRequested();
                    }
                }

                if (Process.ExitCode != 0)
                {
                    Errors = String.Join(Environment.NewLine, errors.ToArray());
                    RaiseOnEnd();
                    throw new ProcessException(this);
                }
            }
            RaiseOnEnd();

            return GetResult();
        }

        protected virtual TResults GetResult()
        {
            if (outputProcessor != null)
                return outputProcessor.Result;
            return (TResults)(object)(Process.StartInfo.CreateNoWindow ? "Process finished" : "Process running");
        }

        private bool WaitForExit(int milliseconds)
        {
            //logger.Debug("WaitForExit - time: {0}ms", milliseconds);

            // Workaround for a bug in which some data may still be processed AFTER this method returns true, thus losing the data.
            // http://connect.microsoft.com/VisualStudio/feedback/details/272125/waitforexit-and-waitforexit-int32-provide-different-and-undocumented-implementations
            bool waitSucceeded = Process.WaitForExit(milliseconds);
            if (waitSucceeded)
            {
                Process.WaitForExit();
            }
            return waitSucceeded;
        }

        protected Process Process { get; set; }
        public int ProcessId { get { return Process.Id; } }
        public override bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Process.ExitCode == 0; } }
        public StreamWriter StandardInput { get { return input; } }
        public virtual string ProcessName { get; }
        public virtual string ProcessArguments { get; }
    }
}