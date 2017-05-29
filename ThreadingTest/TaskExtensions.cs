using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class TaskExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task)
        {
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this ITask task)
        {
            task.Task.Forget();
        }

        //http://stackoverflow.com/a/29491927
        public static Action<T> Debounce<T>(this Action<T> func, int milliseconds = 300)
        {
            var last = 0;
            return arg =>
            {
                var current = Interlocked.Increment(ref last);
                TaskEx.Delay(milliseconds).ContinueWith(task =>
                {
                    if (current == last) func(arg);
                    task.Dispose();
                });
            };
        }

        public static Action Debounce(this Action func, int milliseconds = 300)
        {
            var last = 0;
            return () =>
            {
                var current = Interlocked.Increment(ref last);
                TaskEx.Delay(milliseconds).ContinueWith(task =>
                {
                    if (current == last) func();
                    task.Dispose();
                });
            };
        }

        public static T Catch<T>(this T task, Action<Exception> handler)
        where T : ITask
        {
            Guard.ArgumentNotNull(handler, "handler");
            task.Task.ContinueWith(t =>
            {
                handler(t.Exception is AggregateException ? t.Exception.InnerException : t.Exception);
            },
                task.Token,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskManager.GetScheduler(task.Affinity));
            return task;
        }

        public static ITask Then(this ITask task, Action<bool> continuation, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new ActionTask(task.Token, continuation, task, always) { Name = "Then" };
        }

        public static ITask ThenInUI(this ITask task, Action<bool> continuation, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new ActionTask(task.Token, continuation, task, always) { Affinity = TaskAffinity.UI, Name = "ThenInUI" };
        }

        public static ActionTask<T> Then<T>(this ITask<T> task, Action<bool, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new ActionTask<T>(task.Token, continuation, task, always) { Affinity = affinity, Name = "Then" };
        }

        public static FuncTask<TResult, T> Then<TResult, T>(this ITask<TResult> task, Func<bool, TResult, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new FuncTask<TResult, T>(task.Token, continuation, task) { Affinity = affinity, Name = "FuncTask Then" };
        }

        public static ActionTask<T> ThenInUI<T>(this ITask<T> task, Action<bool, T> continuation, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new ActionTask<T>(task.Token, continuation, task) { Affinity = TaskAffinity.UI };
        }

        public static FuncTask<TResult, T> ThenInUI<TResult, T>(this ITask<TResult> task, Func<bool, TResult, T> continuation, bool always = false)
        {
            Guard.ArgumentNotNull(continuation, "continuation");
            return new FuncTask<TResult, T>(task.Token, continuation, task) { Affinity = TaskAffinity.UI };
        }
    }
}