using System;
using System.Threading;
using System.Threading.Tasks;

namespace ThreadingTest
{
    class ActionTask<TResult, TDependentResult> : TaskBase<TResult>
    {
        private readonly Func<TDependentResult, TResult> action;
        private readonly Func<TResult> actionIfDependentFailed;

        public ActionTask(CancellationToken token, Func<TDependentResult, TResult> action, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.action = action;
            Task = new Task<TResult>(Run, new Lazy<TDependentResult>(() => dependsOn.Result), Token, TaskCreationOptions.None);
        }

        public ActionTask(CancellationToken token, Func<TDependentResult, TResult> action, Func<TResult> actionIfDependentFailed, ITask<TDependentResult> dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.action = action;
            this.actionIfDependentFailed = actionIfDependentFailed;
            Task = new Task<TResult>(Run, new Lazy<TDependentResult>(() => dependsOn.Result), Token, TaskCreationOptions.None);
        }

        public ActionTask(CancellationToken token, Func<TDependentResult, TResult> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            var dependsOnTyped = dependsOn as ITask<TDependentResult>;
            if (dependsOnTyped == null)
            {
                throw new ArgumentException(String.Format("The dependent task result type {0} does not match {1}", dependsOn.GetType().GetGenericArguments(), typeof(TDependentResult)), "dependsOn");
            }

            this.action = action;
            Task = new Task<TResult>(Run, new Lazy<TDependentResult>(() => dependsOnTyped.Result), Token, TaskCreationOptions.None);
        }

        public ActionTask(CancellationToken token, Func<TDependentResult, TResult> action, Func<TResult> actionIfDependentFailed, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            var dependsOnTyped = dependsOn as ITask<TDependentResult>;
            if (dependsOnTyped == null)
            {
                throw new ArgumentException(String.Format("The dependent task result type {0} does not match {1}", dependsOn.GetType().GetGenericArguments(), typeof(TDependentResult)), "dependsOn");
            }
            this.action = action;
            this.actionIfDependentFailed = actionIfDependentFailed;
            Task = new Task<TResult>(Run, new Lazy<TDependentResult>(() => dependsOnTyped.Result), Token, TaskCreationOptions.None);
        }

        private TResult Run(object data)
        {
            if (actionIfDependentFailed == null)
                WaitForDependentTask();

            RaiseOnStart();

            TResult result = default(TResult);
            if (DependsOn != null && !DependsOn.Successful)
            {
                if (actionIfDependentFailed != null)
                    result = actionIfDependentFailed();
            }
            else
            {
                var previousResult = (data as Lazy<TDependentResult>).Value;
                result = action(previousResult);
            }
            RaiseOnEnd();
            return result;
        }
    }

    class ActionTask<TResult> : TaskBase<TResult>
    {
        private readonly Func<TResult> action;
        private readonly Func<TResult> actionIfDependentFailed;

        public ActionTask(CancellationToken token, Func<TResult> action) : base(token)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");

            this.action = action;
            Task = new Task<TResult>(Run, Token, TaskCreationOptions.None);
        }

        public ActionTask(CancellationToken token, Func<TResult> action, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.action = action;
            Task = new Task<TResult>(Run, Token, TaskCreationOptions.None);
        }

        public ActionTask(CancellationToken token, Func<TResult> action, Func<TResult> actionIfDependentFailed, ITask dependsOn)
            : base(token, dependsOn)
        {
            Guard.ArgumentNotNull(token, "token");
            Guard.ArgumentNotNull(action, "action");
            Guard.ArgumentNotNull(actionIfDependentFailed, "actionIfDependentFailed");
            Guard.ArgumentNotNull(dependsOn, "dependsOn");

            this.action = action;
            this.actionIfDependentFailed = actionIfDependentFailed;
            Task = new Task<TResult>(Run, Token, TaskCreationOptions.None);
        }

        private TResult Run()
        {
            if (actionIfDependentFailed == null)
                WaitForDependentTask();

            RaiseOnStart();

            TResult result = default(TResult);
            if (DependsOn != null && !DependsOn.Successful)
            {
                result = actionIfDependentFailed();
            }
            else
            {
                result = action();
            }
            RaiseOnEnd();
            return result;
        }
    }
}