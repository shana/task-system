using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;

namespace GitHub.Unity
{
    class TaskManager
    {
        private readonly TaskScheduler uiScheduler;
        private readonly CancellationTokenSource cts;
        private readonly ConcurrentExclusiveInterleave manager;
        public TaskScheduler UIScheduler { get { return uiScheduler; } }
        public TaskScheduler ConcurrentScheduler { get { return manager.ConcurrentTaskScheduler; } }
        public TaskScheduler ExclusiveScheduler { get { return manager.ExclusiveTaskScheduler; } }

        public TaskManager(TaskScheduler uiScheduler, CancellationTokenSource cts)
        {
            this.manager = new ConcurrentExclusiveInterleave(cts.Token);
            this.uiScheduler = uiScheduler;
            this.cts = cts;
        }

        public void Stop()
        {
            cts.Cancel();
            manager.Wait();
        }

        public void Schedule(params ITask[] tasks)
        {
            Guard.ArgumentNotNull(tasks, "tasks");

            var enumerator = tasks.GetEnumerator();
            bool isLast = !enumerator.MoveNext();
            do
            {
                var task = enumerator.Current as ITask;
                isLast = !enumerator.MoveNext();
                Schedule(task, isLast);
            } while (!isLast);
        }

        public void Schedule(ITask task)
        {
            Schedule(task, true);
        }

        private void Schedule(ITask task, bool setupFaultHandler)
        {
            switch (task.Affinity)
            {
                case TaskAffinity.Exclusive:
                    ScheduleExclusive(task, setupFaultHandler);
                    break;
                case TaskAffinity.UI:
                    ScheduleUI(task, setupFaultHandler);
                    break;
                case TaskAffinity.Concurrent:
                default:
                    ScheduleConcurrent(task, setupFaultHandler);
                    break;
            }
        }

        public void ScheduleUI(ITask task)
        {
            ScheduleUI(task, true);
        }

        private void ScheduleUI(ITask task, bool setupFaultHandler)
        {
            if (setupFaultHandler)
            {
                task.Task.ContinueWith(tt =>
                {
                    Console.WriteLine(String.Format("Exception ui! thread: {0} {1} {2}", Thread.CurrentThread.ManagedThreadId, tt.Id, tt.Exception.InnerException));
                },
                    cts.Token,
                    TaskContinuationOptions.OnlyOnFaulted, uiScheduler
                    );
            }
            Console.WriteLine(String.Format("Schedule {0} {1}", task.Affinity, task.Task.Id));
            task.Start(uiScheduler);
        }

        public void ScheduleExclusive(ITask task)
        {
            ScheduleExclusive(task, true);
        }

        private void ScheduleExclusive(ITask task, bool setupFaultHandler)
        {
            if (setupFaultHandler)
            {
                task.Task.ContinueWith(tt =>
                {
                    Console.WriteLine(String.Format("Exception exclusive! thread: {0} {1} {2}", Thread.CurrentThread.ManagedThreadId, tt.Id, tt.Exception.InnerException));
                },
                    cts.Token,
                    TaskContinuationOptions.OnlyOnFaulted, uiScheduler
                    );
            }
            Console.WriteLine(String.Format("Schedule {0} {1}", task.Affinity, task.Task.Id));
            task.Start(manager.ExclusiveTaskScheduler);
        }

        public void ScheduleConcurrent(ITask task)
        {
            ScheduleConcurrent(task, true);
        }

        private void ScheduleConcurrent(ITask task, bool setupFaultHandler)
        {
            if (setupFaultHandler)
            {
                task.Task.ContinueWith(tt =>
                {
                    Console.WriteLine(String.Format("Exception concurrent! thread: {0} {1} {2}", Thread.CurrentThread.ManagedThreadId, tt.Id, tt.Exception.InnerException));
                },
                    cts.Token,
                    TaskContinuationOptions.OnlyOnFaulted, uiScheduler
                    );
            }
            Console.WriteLine(String.Format("Schedule {0} {1}", task.Affinity, task.Task.Id));
            task.Start(manager.ConcurrentTaskScheduler);
        }
    }
}