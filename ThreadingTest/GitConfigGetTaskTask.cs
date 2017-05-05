using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitConfigGetTaskTask : ProcessTaskTask<List<KeyValuePair<string, string>>, KeyValuePair<string, string>>
    {
        private readonly static ConfigOutputProcessor defaultProcessor = new ConfigOutputProcessor();
        public GitConfigGetTaskTask(ProcessManager processManager, string key, ConfigOutputProcessor processor = null) : base(processManager.Token, processor ?? defaultProcessor)
        {
            Guard.ArgumentNotNull(processManager, "processManager");

            this.Name = "git config";
            var psi = processManager.Configure("git", String.Format("config --get {0}", key));
            Configure(psi);
        }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}