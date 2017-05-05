using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadingTest
{
    interface ITask : IAsyncResult
    {
        ITask Start(TaskScheduler scheduler);
        void Wait();
        bool Wait(int milliseconds);
        bool Successful { get; }
        string Errors { get; }
        Task Task { get; }
        string Name { get; }
        TaskAffinity Affinity { get; }
        event Action<ITask> OnStart;
        event Action<ITask> OnEnd;
    }

    interface ITask<T> : ITask
    {
        new ITask<T> Start(TaskScheduler scheduler);
        T Result { get; }
        new Task<T> Task { get; }
        new event Action<ITask<T>> OnStart;
        new event Action<ITask<T>> OnEnd;
    }

    interface ITask<T, TData> : ITask<T>
    {
        event Action<TData> OnData;
    }

    abstract class TaskBase<T, TData> : TaskBase<T>, ITask<T, TData>
    {
        public TaskBase(CancellationToken token) : base(token)
        {}

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    abstract class TaskBase<T> : TaskBase, ITask<T>
    {
        public TaskBase(CancellationToken token) : base(token)
        {}

        public TaskBase(CancellationToken token, ITask dependsOn) : base(token, dependsOn)
        {}

        public new ITask<T> Start(TaskScheduler scheduler)
        {
            Task.Start(scheduler);
            return this;
        }

        public new event Action<ITask<T>> OnStart;
        public new event Action<ITask<T>> OnEnd;
        public new Task<T> Task
        {
            get { return base.Task as Task<T>; }
            set { base.Task = value; }
        }
        public T Result { get { return Task.Result; } }

        protected void RaiseOnStart()
        {
            OnStart?.Invoke(this);
        }

        protected void RaiseOnEnd()
        {
            OnEnd?.Invoke(this);
        }
    }

    abstract class TaskBase : ITask
    {
        private readonly ITask dependsOn;
        protected ITask DependsOn { get { return dependsOn; } }

        private readonly CancellationToken token;
        protected CancellationToken Token { get { return token; } }

        public TaskBase(CancellationToken token)
        {
            this.token = token;
        }

        public TaskBase(CancellationToken token, ITask dependsOn)
            : this(token)
        {
            this.dependsOn = dependsOn;
        }

        public virtual ITask Start(TaskScheduler scheduler)
        {
            Task.Start(scheduler);
            return this;
        }

        public virtual void Wait()
        {
            Task.Wait(token);
        }

        public virtual bool Wait(int milliseconds)
        {
            return Task.Wait(milliseconds, token);
        }

        protected void WaitForDependentTask()
        {
            if (DependsOn != null)
            {
                try
                {
                    DependsOn.Task.Wait();
                }
                catch(AggregateException ex)
                {
                    throw new DependentTaskFailedException(DependsOn, ex.InnerException);
                }
            }
        }

        public virtual bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Task.Status != TaskStatus.Faulted; } }
        public string Errors { get; protected set; }
        public Task Task { get; protected set; }
        public string Name { get; set; }

        public event Action<ITask> OnStart;
        public event Action<ITask> OnEnd;

        public bool IsCompleted { get { return (Task as IAsyncResult).IsCompleted; } }

        public WaitHandle AsyncWaitHandle { get { return (Task as IAsyncResult).AsyncWaitHandle; } }

        public object AsyncState { get { return (Task as IAsyncResult).AsyncState; } }

        public bool CompletedSynchronously { get { return (Task as IAsyncResult).CompletedSynchronously; } }
        public virtual TaskAffinity Affinity { get; set; }
    }

    enum TaskAffinity
    {
        Concurrent,
        Exclusive,
        UI
    }
}