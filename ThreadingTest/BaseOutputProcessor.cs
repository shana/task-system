using System;

namespace ThreadingTest
{
    interface IOutputProcessor<TResults, TResultEntry>
    {
        void LineReceived(string line);
        TResults Result { get; }
        event Action<TResultEntry> OnEntry;
    }

    abstract class BaseOutputProcessor<TResult> : IOutputProcessor<TResult, TResult>
    {
        public abstract void LineReceived(string line);
        public TResult Result { get; protected set; }
        public event Action<TResult> OnEntry;
        protected void RaiseOnEntry(TResult entry)
        {
            OnEntry?.Invoke(entry);
        }
    }

    abstract class BaseOutputProcessor<TResults, TResultEntry> : IOutputProcessor<TResults, TResultEntry>
    {
        public abstract void LineReceived(string line);
        public TResults Result { get; protected set; }
        public event Action<TResultEntry> OnEntry;
        protected void RaiseOnEntry(TResultEntry entry)
        {
            OnEntry?.Invoke(entry);
        }
    }
}