using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Threading;
using GitHub.Unity;
using System.Threading.Tasks.Schedulers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IntegrationTests
{
    class BaseTest
    {
        protected ITaskManager TaskManager { get; set; }
        protected IProcessManager ProcessManager { get; set; }
        protected CancellationTokenSource CancelTokenSource { get; set; }
        [SetUp]
        public void Setup()
        {
            
            CancelTokenSource = new CancellationTokenSource();
            ProcessManager = new ProcessManager(new DefaultEnvironment(), new ProcessEnvironment(), CancelTokenSource.Token);
            var syncContext = new ThreadSynchronizationContext(CancelTokenSource.Token);
            TaskManager = new TaskManager(new SynchronizationContextTaskScheduler(syncContext), CancelTokenSource);
        }
    }

    [TestFixture]
    class ProcessTaskTests : BaseTest
    {
        [Test]
        public async Task GitTest()
        {
            var done = false;
            var output = new List<string>();
            var first = new GitConfigGetTask("user.name", GitConfigSource.NonSpecified, CancelTokenSource.Token)
                        { Affinity = TaskAffinity.UI }.ConfigureGitProcess(ProcessManager);

            var rest = first
                .ContinueWith((s, d) =>
                {
                    return d;
                })
                .ContinueWith((s, d) =>
                {
                    if (s)
                        output.Add(d);
                })
                .ContinueWith(new GitConfigListTask(GitConfigSource.Global, CancelTokenSource.Token)
                        { Affinity = TaskAffinity.Exclusive }.ConfigureGitProcess(ProcessManager))
                .ContinueWith((s, d) =>
                {
                    if (s)
                        output.AddRange(d.Select(kvp => kvp.Key + ":" + kvp.Value));
                })
                .ContinueWithUI(s => done = true);

            first.Start();
            await rest.Task;

            Assert.IsTrue(done);
            Console.WriteLine(String.Join(",", output.ToArray()));
        }
    }

    [TestFixture]
    class ActionTaskTests : BaseTest
    {
        
    }

    [TestFixture]
    class FuncTaskTests : BaseTest
    {

    }
}
