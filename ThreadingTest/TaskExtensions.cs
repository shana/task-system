using System;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    static class TaskExtensions
    {
        public static async Task<T> Catch<T>(this Task<T> source, Func<Exception, T> handler = null)
        {
            try
            {
                return await source;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    return handler(ex);
                return default(T);
            }
        }

        public static async Task Catch(this Task source, Action<Exception> handler = null)
        {
            try
            {
                await source;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    handler(ex);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this Task task)
        {
        }

        public static async Task<ITask<T>> Catch<T>(this ITask<T> source, Func<ITask<T>, Exception, ITask<T>> handler = null)
        {
            try
            {
                await source.Task;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    return handler(source, ex);
            }
            return source;
        }

        public static async Task<ITask> Catch(this ITask source, Action<ITask, Exception> handler = null)
        {
            try
            {
                await source.Task;
            }
            catch (Exception ex)
            {
                if (handler != null)
                    handler(source, ex);
            }
            return source;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "task")]
        public static void Forget(this ITask task)
        {
            task.Task.Forget();
        }
    }
}