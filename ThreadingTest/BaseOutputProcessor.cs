using System;
using System.Collections.Generic;
using System.Text;

namespace GitHub.Unity
{
    interface IOutputProcessor
    {
        void LineReceived(string line);
    }

    interface IOutputProcessor<TResult> : IOutputProcessor
    {
        TResult Result { get; }
        event Action<TResult> OnEntry;
    }

    interface IOutputProcessor<TResults, TResultEntry> : IOutputProcessor
    {
        TResults Result { get; }
        event Action<TResultEntry> OnEntry;
    }

    abstract class BaseOutputProcessor<TResult> : IOutputProcessor<TResult>
    {
        private ILogging logger;
        protected ILogging Logger
        {
            get
            {
                if (logger == null)
                    logger = Logging.GetLogger(GetType());
                return logger;
            }
        }

        public event Action<TResult> OnEntry;

        public abstract void LineReceived(string line);
        protected void RaiseOnEntry(TResult entry)
        {
            Result = entry;
            OnEntry?.Invoke(entry);
        }
        public virtual TResult Result { get; protected set; }
    }

    abstract class BaseOutputProcessor<TResults, TResultEntry> : IOutputProcessor<TResults, TResultEntry>
          where TResults : new()
    {
        private ILogging logger;
        protected ILogging Logger
        {
            get
            {
                if (logger == null)
                    logger = Logging.GetLogger(GetType());
                return logger;
            }
        }

        public event Action<TResultEntry> OnEntry;

        public abstract void LineReceived(string line);
        protected void RaiseOnEntry(TResultEntry entry)
        {
            OnEntry?.Invoke(entry);
        }
        public virtual TResults Result { get; protected set; }
    }

    abstract class BaseOutputListProcessor<TResult> : IOutputProcessor<List<TResult>, TResult>
    {
        private ILogging logger;
        protected ILogging Logger
        {
            get
            {
                if (logger == null)
                    logger = Logging.GetLogger(GetType());
                return logger;
            }
        }

        public event Action<TResult> OnEntry;

        public abstract void LineReceived(string line);
        protected void RaiseOnEntry(TResult entry)
        {
            if (Result == null)
            {
                Result = new List<TResult>();
            }
            Result.Add(entry);
            OnEntry?.Invoke(entry);
        }
        public virtual List<TResult> Result { get; protected set; }
    }

    class SimpleOutputProcessor : BaseOutputProcessor<string>
    {
        private readonly StringBuilder sb = new StringBuilder();
        public override void LineReceived(string line)
        {
            if (line == null)
                return;
            sb.AppendLine(line);
            RaiseOnEntry(line);
        }
        public override string Result { get { return sb.ToString(); } }
    }
}