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
    interface IProcessTask<T> : ITask<T>
    {
        event Action<string> OnErrorData;
        int ProcessId { get; }
        StreamWriter StandardInput { get; }
    }


    class ProcessTaskTask<T> : ProcessTaskTask<T, T>
    {
        public ProcessTaskTask(CancellationToken token, IOutputProcessor<T, T> outputProcessor)
            : base(token, outputProcessor)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResults">The type of the results. If it's a List<> or similar, then specify the full List<> type here and the inner type of the List in <typeparam name="TResult"/>
    /// <typeparam name="TResult">If <typeparam name="TResult"/> is a list or similar, then specify its inner type here</typeparam>
    class ProcessTaskTask<TResults, TResult> : TaskBase<TResults, TResult>, IProcessTask<TResults>
    {
        private IOutputProcessor<TResults, TResult> outputProcessor;
        private Process process;

        private List<string> errors;
        private StreamWriter input;

        public event Action<string> OnErrorData;

        public ProcessTaskTask(CancellationToken token, IOutputProcessor<TResults, TResult> outputProcessor = null)
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
}