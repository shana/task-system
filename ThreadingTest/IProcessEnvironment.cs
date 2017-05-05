using System;
using System.Diagnostics;

namespace GitHub.Unity
{
    internal interface IProcessEnvironment
    {
        void Configure(ProcessStartInfo startInfo, string workingDirectory);
    }

    class ProcessEnvironment : IProcessEnvironment
    {
        public void Configure(ProcessStartInfo startInfo, string workingDirectory)
        {
            
        }
    }
}