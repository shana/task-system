using System.Collections.Generic;

namespace GitHub.Unity
{
    class GitConfigListTaskTask : ProcessTaskTask<List<KeyValuePair<string, string>>, KeyValuePair<string, string>>
    {
        private readonly static ConfigOutputProcessor defaultProcessor = new ConfigOutputProcessor();
        public GitConfigListTaskTask(ProcessManager processManager, ConfigOutputProcessor processor = null) : base(processManager.Token, processor ?? defaultProcessor)
        {
            Guard.ArgumentNotNull(processManager, "processManager");

            this.Name = "git config";
            var psi = processManager.Configure("git", "config -l --show-origin");
            Configure(psi);
        }

        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
    }
}