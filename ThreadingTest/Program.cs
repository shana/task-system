﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    {
                        //var task = new GitConfigListTask(GitConfigSource.NonSpecified, cts.Token).Configure(processManager);

                        //var other = new FuncTask<List<KeyValuePair<string, string>>, bool>(cts.Token, d =>
                        //{
                        //    Console.WriteLine("Results! {0}", d.Count);
                        //    Thread.Sleep(1000);
                        //    return true;
                        //}, task);
                        //other.Name = "Other";
                        //other.Affinity = TaskAffinity.Concurrent;

                        //other.OnStart += tt => Console.WriteLine(String.Format("Executing id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));
                        //other.OnEnd += tt => Console.WriteLine(String.Format("Finished id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));


                        //var final = new FuncTask<bool, bool>(cts.Token, d => d, other);
                        //final.Name = "Final";
                        //final.Affinity = TaskAffinity.UI;

                        //final.OnStart += tt => Console.WriteLine(String.Format("Executing id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));
                        //final.OnEnd += tt => Console.WriteLine(String.Format("Finished id:{0} thread:{1}", tt.Task.Id, Thread.CurrentThread.ManagedThreadId));

                        //taskManager.Schedule(task, other, final);
                    }

                    {
                        //var task = new GitConfigListTask(GitConfigSource.NonSpecified, cts.Token)
                        //    .ConfigureGitProcess(processManager)
                        //    .Schedule(taskManager);
                        //task
                        //    .ContinueWithUI((success, result) => Console.WriteLine("{0} Result? {1} {2}", Thread.CurrentThread.ManagedThreadId, success, 
                        //        success ? String.Join(";", result.Take(10).Select(x => x.Key + "=" + x.Value).ToArray()) : "error", false));
                    }

                    {
                        //var task = new GitConfigGetTask("user.name", GitConfigSource.NonSpecified, cts.Token)
                        //    .ConfigureGitProcess(processManager)
                            
                        //    .Schedule(taskManager);
                        //task
                        //    .ContinueWithUI((success, result) =>
                        //    {
                        //        Console.WriteLine("{0} Result? {1} {2}", Thread.CurrentThread.ManagedThreadId, success, result);
                        //        return 1;
                        //    }, true);
                    }

                    {
                        //var task = new GitConfigGetTask("user.name", GitConfigSource.NonSpecified, cts.Token).ConfigureGitProcess(processManager);
                        //var other = new GitConfigListTask(GitConfigSource.Global, cts.Token).ConfigureGitProcess(processManager);
                        //var another = new ActionTask(cts.Token, () => Console.WriteLine("And we are done"));
                        //task.Then(other).Then(another);
                        //task.Schedule(taskManager);
                        //other.Schedule(taskManager);
                        //another.Schedule(taskManager);
                    }

                    {
                        for (int i = 0; i < 5; i++)
                        {
                            var first = new GitConfigGetTask("user.name", GitConfigSource.NonSpecified, cts.Token) { Affinity = TaskAffinity.Concurrent }.ConfigureGitProcess(processManager);
                            var second = new GitConfigListTask(GitConfigSource.Global, cts.Token) { Affinity = TaskAffinity.Concurrent }.ConfigureGitProcess(processManager);
                            var third = new ActionTask(cts.Token, _ => Console.WriteLine("And we are done")) { Affinity = TaskAffinity.UI };
                            second.ContinueWith(third);
                            taskManager.Schedule(first, second);
                        }
                    }
                }
            }

            taskManager.Stop();

            Console.WriteLine("Done");
            Console.Read();
        }
    }
}
