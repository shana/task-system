using System;
using System.Collections.Generic;

namespace ThreadingTest
{
    class ConfigOutputProcessor : BaseOutputProcessor<List<KeyValuePair<string, string>>, KeyValuePair<string, string>>
    {
        public ConfigOutputProcessor()
        {
            Result = new List<KeyValuePair<string, string>>();
        }

        public override void LineReceived(string line)
        {
            if (String.IsNullOrEmpty(line))
                return;

            var eqs = line.IndexOf("=");
            if (eqs <= 0)
            {
                return;
            }
            var kvp = new KeyValuePair<string, string>(line.Substring(0, eqs), line.Substring(eqs + 1));
            RaiseOnEntry(kvp);
            Result.Add(kvp);
        }
    }
}