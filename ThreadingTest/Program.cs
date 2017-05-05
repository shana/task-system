using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks.Schedulers;

namespace GitHub.Unity
{
    class Program
    {
        static void Main(string[] args)
        {

            var cts = new CancellationTokenSource();
            var processManager = new ProcessManager(new DefaultEnvironment(), new ProcessEnvironment(), cts.Token);

            var syncContext = new ThreadSynchronizationContext(cts.Token);
            var taskManager = new TaskManager(new SynchronizationContextTaskScheduler(syncContext), cts);


            ConsoleKey key;
            while ((key = Console.ReadKey().Key) != ConsoleKey.Escape)
            {
                if (key == ConsoleKey.Enter)
                {
                    var task = new GitConfigListTask(GitConfigSource.NonSpecified, cts.Token).Configure(processManager);

                    var other = new ActionTask<bool, List<KeyValuePair<string, string>>>(cts.Token, d =>
                    {
                        Console.WriteLine("Results! {0}", d.Count);
                        Thread.Sleep(1000);
                        return true;
                    }, task);
                    other.Name = "Other";
                    other.Affinity = TaskAffinity.Concurrent;

                    other.OnStart += tt => Console.WriteLine(String.Format("Executing id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));
                    other.OnEnd += tt => Console.WriteLine(String.Format("Finished id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));


                    var final = new ActionTask<bool, bool>(cts.Token, d => d, other);
                    final.Name = "Final";
                    final.Affinity = TaskAffinity.UI;

                    final.OnStart += tt => Console.WriteLine(String.Format("Executing id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));
                    final.OnEnd += tt => Console.WriteLine(String.Format("Finished id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));

                    taskManager.Schedule(task, other, final);
                }
            }

            taskManager.Stop();

            Console.WriteLine("Done");
            Console.Read();
        }
    }
}
