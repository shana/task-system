using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface ITask : IAsyncResult
    {
        T Then<T>(T continuation, bool always = false) where T : ITask;
        // Continues and also sends a flag indicating whether the current task was successful or not
        ITask Finally(Action<bool, Exception> continuation, TaskAffinity affinity = TaskAffinity.Concurrent);
        ITask SetDependsOn(ITask dependsOn);
        ITask Start();
        ITask Start(TaskScheduler scheduler);

        void Wait();
        bool Wait(int milliseconds);
        bool Successful { get; }
        string Errors { get; }
        Task Task { get; }
        string Name { get; }
        TaskAffinity Affinity { get; }
        CancellationToken Token { get; }
        TaskBase DependsOn { get; }
        event Action<ITask> OnStart;
        event Action<ITask> OnEnd;
    }

    interface ITask<TResult> : ITask
    {
        new ITask<TResult> Start();
        new ITask<TResult> Start(TaskScheduler scheduler);
        TResult Result { get; }
        new Task<TResult> Task { get; }
        new event Action<ITask<TResult>> OnStart;
        new event Action<ITask<TResult>> OnEnd;
    }

    interface ITask<TData, T> : ITask<T>
    {
        event Action<TData> OnData;
    }

    abstract class TaskBase : ITask
    {
        protected const TaskContinuationOptions runAlwaysOptions = TaskContinuationOptions.None;
        protected const TaskContinuationOptions runOnSuccessOptions = TaskContinuationOptions.OnlyOnRanToCompletion;
        protected const TaskContinuationOptions runOnFaultOptions = TaskContinuationOptions.OnlyOnFaulted;

        public event Action<ITask> OnStart;
        public event Action<ITask> OnEnd;

        protected bool previousSuccess = true;
        protected Exception previousException;
        protected object previousResult;

        private TaskBase continuation;
        private bool continuationAlways;

        public TaskBase(CancellationToken token, ITask dependsOn = null, bool always = false)
        {
            Guard.ArgumentNotNull(token, "token");

            Token = token;
            Task = new Task(() => Run(DependsOn?.Successful ?? previousSuccess), Token, TaskCreationOptions.None);
            dependsOn?.Then(this, always);
        }

        public TaskBase(Task task)
        {
            Task = task;
        }

        protected TaskBase()
        {}

        public ITask SetDependsOn(ITask dependsOn)
        {
            DependsOn = (TaskBase)dependsOn;
            return this;
        }

        public T Then<T>(T continuation, bool always = false)
            where T : ITask
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            continuation.SetDependsOn(this);
            this.continuation = (TaskBase)(object)continuation;
            this.continuationAlways = always;
            return continuation;
        }

        public ITask Finally(Action<bool, Exception> continuation, TaskAffinity affinity = TaskAffinity.Concurrent)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            var ret = new ActionTask(Token, continuation, this, true) { Affinity = affinity, Name = "Finally" };
            DependsOn?.SetFaultHandler(ret);
            return ret;
        }

        private void SetFaultHandler(ActionTask handler)
        {
            Task.ContinueWith(t => handler.Start(t), Token,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskManager.GetScheduler(handler.Affinity));
            DependsOn?.SetFaultHandler(handler);
        }

        public virtual ITask Start()
        {
            var depends = GetFirstDepends();
            if (depends != null)
            {
                depends.Run();
                return this;
            }
            else
            {
                return TaskManager.Instance.Schedule(this);
            }
        }

        protected void Run()
        {
            if (Task.Status == TaskStatus.Created)
            {
                TaskManager.Instance.Schedule(this);
            }
            else
            {
                if (continuation != null)
                {
                    Task.ContinueWith(_ => ((TaskBase)(object)continuation).Run(), Token,
                    continuationAlways ? runAlwaysOptions : runOnSuccessOptions,
                    TaskManager.GetScheduler(continuation.Affinity));
                }
            }
        }

        protected void Start(Task task)
        {
            previousSuccess = task.Status == TaskStatus.RanToCompletion && task.Status != TaskStatus.Faulted;
            previousException = task.Exception;
            Task.Start(TaskManager.GetScheduler(Affinity));
        }

        public virtual ITask Start(TaskScheduler scheduler)
        {
            if (Task.Status == TaskStatus.Created)
            {
                Logger.Trace(String.Format($"Starting {Affinity} {Task.Id} {Name}"));
                Task.Start(scheduler);
            }

            if (continuation != null)
            {
                Task.ContinueWith(_ => ((TaskBase)(object)continuation).Run(), Token,
                    continuationAlways ? runAlwaysOptions : runOnSuccessOptions,
                    TaskManager.GetScheduler(continuation.Affinity));
            }
            return this;
        }

        protected TaskBase GetFirstDepends()
        {
            var depends = DependsOn;
            if (depends == null)
                return null;
            return depends.GetFirstDepends(null);
        }

        protected TaskBase GetFirstDepends(TaskBase ret)
        {
            ret = (Task.Status == TaskStatus.Created ? this : ret);
            var depends = DependsOn;
            if (depends == null)
                return ret;
            return depends.GetFirstDepends(ret);
        }

        public virtual void Wait()
        {
            Task.Wait(Token);
        }

        public virtual bool Wait(int milliseconds)
        {
            return Task.Wait(milliseconds, Token);
        }

        protected virtual void Run(bool success)
        {
            Logger.Trace(String.Format("Executing {0}({1}) success?:{2}", Name, Task.Id, success));
        }

        protected virtual void RaiseOnStart()
        {
            OnStart?.Invoke(this);
        }

        protected virtual void RaiseOnEnd()
        {
            OnEnd?.Invoke(this);
        }

        protected AggregateException GetThrownException()
        {
            if (DependsOn == null)
                return null;

            if (DependsOn.Task.Status != TaskStatus.Created && !DependsOn.Successful)
            {
                return DependsOn.Task.Exception;
            }
            return DependsOn.GetThrownException();
        }

        public virtual bool Successful { get { return Task.Status == TaskStatus.RanToCompletion && Task.Status != TaskStatus.Faulted; } }
        public string Errors { get; protected set; }
        public Task Task { get; protected set; }

        public bool IsCompleted { get { return (Task as IAsyncResult).IsCompleted; } }

        public WaitHandle AsyncWaitHandle { get { return (Task as IAsyncResult).AsyncWaitHandle; } }

        public object AsyncState { get { return (Task as IAsyncResult).AsyncState; } }

        public bool CompletedSynchronously { get { return (Task as IAsyncResult).CompletedSynchronously; } }
        public virtual string Name { get; set; }
        public virtual TaskAffinity Affinity { get; set; }

        private ILogging logger;
        protected ILogging Logger { get { return logger = logger ?? Logging.GetLogger(GetType()); } }
        public TaskBase DependsOn { get; private set; }
        public CancellationToken Token { get; }
    }

    abstract class TaskBase<TResult> : TaskBase, ITask<TResult>
    {
        public TaskBase(CancellationToken token, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Task = new Task<TResult>(() => RunWithReturn(DependsOn?.Successful ?? previousSuccess), Token, TaskCreationOptions.None);
        }

        public TaskBase(Task<TResult> task)
        {
            Task = task;
        }

        public new ITask<TResult> Start()
        {
            base.Start();
            return this;
        }

        public new ITask<TResult> Start(TaskScheduler scheduler)
        {
            base.Start(scheduler);
            return this;
        }

        protected virtual TResult RunWithReturn(bool success)
        {
            base.Run(success);
            return default(TResult);
        }

        public new event Action<ITask<TResult>> OnStart;
        public new event Action<ITask<TResult>> OnEnd;
        public new Task<TResult> Task
        {
            get { return base.Task as Task<TResult>; }
            set { base.Task = value; }
        }
        public TResult Result { get { return Task.Result; } }
    }

    abstract class TaskBase<T, TResult> : TaskBase<TResult>
    {
        public TaskBase(CancellationToken token, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Task = new Task<TResult>(() => RunWithData(DependsOn?.Successful ?? previousSuccess, DependsOn.Successful ? ((ITask<T>)DependsOn).Result : default(T)),
                Token, TaskCreationOptions.None);
        }

        public TaskBase(Task<TResult> task)
            : base(task)
        {}

        protected virtual TResult RunWithData(bool success, T previousResult)
        {
            base.Run(success);
            return default(TResult);
        }
    }

    abstract class DataTaskBase<TData, TResult> : TaskBase<TResult>, ITask<TData, TResult>
    {
        public DataTaskBase(CancellationToken token, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always) { }

        public DataTaskBase(Task<TResult> task)
            : base(task)
        {}

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    abstract class DataTaskBase<T, TData, TResult> : TaskBase<T, TResult>, ITask<TData, TResult>
    {
        public DataTaskBase(CancellationToken token, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always) {}

        public DataTaskBase(Task<TResult> task)
            : base(task)
        {}

        public event Action<TData> OnData;
        protected void RaiseOnData(TData data)
        {
            OnData?.Invoke(data);
        }
    }

    static class TaskBaseExtensions
    {
        public static T Schedule<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.Schedule(task);
        }

        public static T ScheduleUI<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.ScheduleUI(task);
        }

        public static T ScheduleExclusive<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.ScheduleExclusive(task);
        }

        public static T ScheduleConcurrent<T>(this T task, ITaskManager taskManager)
            where T : ITask
        {
            return taskManager.ScheduleConcurrent(task);
        }
    }

    enum TaskAffinity
    {
        Concurrent,
        Exclusive,
        UI
    }
}