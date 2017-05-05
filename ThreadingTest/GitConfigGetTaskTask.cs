using System;
using System.Collections.Generic;
using System.Threading;

namespace GitHub.Unity
{
    class GitConfigGetTask : ProcessTaskWithListOutput<KeyValuePair<string, string>>
    {
        private readonly string arguments;

        public GitConfigGetTask(CancellationToken token,
            string key, GitConfigSource configSource)
            : base(token, new ConfigOutputProcessor())
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        public override string Name { get { return "git config get"; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}
