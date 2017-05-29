using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class ActionTask : TaskBase
    {
        protected Action<bool> Callback { get; }
        protected Action<bool, Exception> CallbackWithException { get; }

        public ActionTask(CancellationToken token, Action<bool> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public ActionTask(CancellationToken token, Action<bool, Exception> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.CallbackWithException = action;
        }

        public ActionTask(Task task)
            : base(task)
        {}

        protected override void Run(bool success)
        {
            base.Run(success);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                Callback?.Invoke(success);
                if (CallbackWithException != null)
                {
                    Exception thrown = GetThrownException();
                    thrown = thrown != null ? thrown.InnerException : thrown;
                    CallbackWithException?.Invoke(success, thrown);
                }
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;
        }
    }

    class ActionTask<T> : TaskBase
    {
        protected Action<bool, T> Callback { get; }

        public ActionTask(CancellationToken token, Action<bool, T> action, ITask<T> dependsOn, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
            Task = new Task(() => Run(DependsOn.Successful, DependsOn.Successful ? ((ITask<T>)DependsOn).Result : default(T)),
                Token, TaskCreationOptions.None);
        }

        protected virtual void Run(bool success, T previousResult)
        {
            base.Run(success);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                Callback?.Invoke(success, previousResult);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;
        }
    }

    class FuncTask<T> : TaskBase<T>
    {
        protected Func<bool, T> Callback { get; }

        public FuncTask(CancellationToken token, Func<bool, T> action, ITask dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override T RunWithReturn(bool success)
        {
            T result = base.RunWithReturn(success);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                result = Callback(success);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            return result;
        }
    }

    class FuncTask<T, TResult> : TaskBase<T, TResult>
    {
        protected Func<bool, T, TResult> Callback { get; }

        public FuncTask(CancellationToken token, Func<bool, T, TResult> action, ITask<T> dependsOn = null, bool always = false)
            : base(token, dependsOn, always)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override TResult RunWithData(bool success, T previousResult)
        {
            var result = base.RunWithData(success, previousResult);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                result = Callback(success, previousResult);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            return result;
        }
    }

    class FuncListTask<T> : ListTaskBase<List<T>, T>
    {
        protected Func<bool, List<T>> Callback { get; }

        public FuncListTask(CancellationToken token, Func<bool, List<T>> action)
            : base(token)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        public FuncListTask(CancellationToken token, Func<bool, List<T>> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override List<T> RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                result = Callback(success);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            if (result == null)
                result = new List<T>();

            return result;
        }
    }

    class FuncListTask<TDependentResult, TResult, TData> : ListTaskBase<TDependentResult, TResult, TData>
    {
        protected Func<bool, TDependentResult, TResult> Callback { get; }

        public FuncListTask(CancellationToken token, Func<bool, TDependentResult, TResult> action, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(action, "action");
            this.Callback = action;
        }

        protected override TResult RunWithData(bool success, TDependentResult previousResult)
        {
            var result = base.RunWithData(success, previousResult);

            RaiseOnStart();
            Exception exception = null;
            try
            {
                result = Callback(success, previousResult);
            }
            catch (Exception ex)
            {
                Errors = ex.Message;
                exception = ex;
            }
            RaiseOnEnd();

            if (exception != null)
                throw exception;

            return result;
        }
    }

}