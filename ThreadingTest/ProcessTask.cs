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
    static class ProcessTaskExtensions
    {
        public static T ConfigureGitProcess<T>(this T task, IProcessManager processManager, bool withInput = false)
            where T : IProcess
        {
            return processManager.ConfigureGitProcess(task, withInput);
        }

        public static T Configure<T>(this T task, IProcessManager processManager, string executable, string arguments, string workingDirectory, bool withInput)
            where T : IProcess
        {
            return processManager.Configure(task, executable, arguments, workingDirectory, withInput);
        }
    }

    interface IProcess
    {
        void Configure(Process existingProcess);
        void Configure(ProcessStartInfo psi);
        event Action<string> OnErrorData;
        StreamWriter StandardInput { get; }
        int ProcessId { get; }
        string ProcessName { get; }
        string ProcessArguments { get; }
        Process Process { get; set; }
    }

    interface IProcessTask<T> : ITask<T>, IProcess
    {
        void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor);
    }

    interface IProcessTask<T, TData> : ITask<T, TData>, IProcess
    {
        void Configure(ProcessStartInfo psi, IOutputProcessor<T, TData> processor);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">The type of the results. If it's a List<> or similar, then specify the full List<> type here and the inner type of the List in <typeparam name="TData"/>
    /// <typeparam name="TData">If <typeparam name="TData"/> is a list or similar, then specify its inner type here</typeparam>
    class ProcessTask<T> : TaskBase<T>, IProcessTask<T>
    {
        private IOutputProcessor<T> outputProcessor;

        private readonly List<string> errors = new List<string>();
        private StreamWriter input;

        public event Action<string> OnErrorData;

        public ProcessTask(CancellationToken token, IOutputProcessor<T> outputProcessor = null, ITask dependsOn = null)
            : base(token)
        {
            Guard.ArgumentNotNull(token, "token");

            this.outputProcessor = outputProcessor;
        }

        /// <summary>
        /// Process that calls git with the passed arguments
        /// </summary>
        /// <param name="token"></param>
        /// <param name="arguments"></param>
        /// <param name="outputProcessor"></param>
        public ProcessTask(CancellationToken token, string arguments, IOutputProcessor<T> outputProcessor = null, ITask dependsOn = null)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");

            this.outputProcessor = outputProcessor;
            ProcessArguments = arguments;
        }

        public virtual void Configure(ProcessStartInfo psi)
        {
            Guard.ArgumentNotNull(psi, "psi");

            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
        }

        public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T> processor)
        {
            outputProcessor = processor ?? outputProcessor;
            ConfigureOutputProcessor();
            Process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            ProcessName = psi.FileName;
        }

        public void Configure(Process existingProcess)
        {
            Guard.ArgumentNotNull(existingProcess, "existingProcess");

            ConfigureOutputProcessor();
            Process = existingProcess;
            ProcessName = existingProcess.StartInfo.FileName;
        }

        protected virtual void ConfigureOutputProcessor()
        {
        }

        protected override T RunWithReturn()
        {
            Logger.Debug(String.Format("Executing id:{0}", Task.Id));

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

        protected virtual T GetResult()
        {
            if (outputProcessor != null)
                return outputProcessor.Result;
            return (T)(object)(Process.StartInfo.CreateNoWindow ? "Process finished" : "Process running");
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

        public Process Process { get; set; }
        public int ProcessId { get { return Process.Id; } }
        public override bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Process.ExitCode == 0; } }
        public StreamWriter StandardInput { get { return input; } }
        public virtual string ProcessName { get; protected set; }
        public virtual string ProcessArguments { get; }
    }

    class ProcessTask<T, TData> : ProcessTask<T>, ITask<T, TData>, IProcessTask<T, TData>
    {
        private IOutputProcessor<T, TData> outputProcessor;
        public event Action<TData> OnData;

        public ProcessTask(CancellationToken token, IOutputProcessor<T, TData> outputProcessor = null, ITask dependsOn = null)
            : base(token, outputProcessor, dependsOn)
        {
            this.outputProcessor = outputProcessor;
        }

        public virtual void Configure(ProcessStartInfo psi, IOutputProcessor<T, TData> processor)
        {
            Guard.ArgumentNotNull(psi, "psi");
            Guard.ArgumentNotNull(processor, "processor");

            outputProcessor = processor ?? outputProcessor;
            base.Configure(psi, outputProcessor as IOutputProcessor<T>);
        }

        protected override void ConfigureOutputProcessor()
        {
            if (outputProcessor == null && (typeof(T) != typeof(string)))
            {
                throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
            }
            outputProcessor.OnEntry += x => RaiseOnData(x);
        }

        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }


    class ProcessTaskWithListOutput<T> : ProcessTask<List<T>, T>
    {
        public ProcessTaskWithListOutput(CancellationToken token, IOutputProcessor<List<T>, T> outputProcessor = null, ITask dependsOn = null)
            : base(token, outputProcessor, dependsOn)
        {
        }
    }
}