using System;
using System.Diagnostics;

namespace GitHub.Unity
{
    internal interface IProcessEnvironment
    {
        void Configure(ProcessStartInfo startInfo, string workingDirectory);
        ITask<NPath> FindGitInstallationPath(IProcessManager processManager);
    }

    class ProcessEnvironment : IProcessEnvironment
    {
        protected IEnvironment Environment { get; private set; }

        public ProcessEnvironment(IEnvironment environment)
        {
            Environment = environment;
        }

        public void Configure(ProcessStartInfo startInfo, string workingDirectory)
        {
        }

        public ITask<NPath> FindGitInstallationPath(IProcessManager processManager)
        {
            return new ProcessTask<NPath>(TaskManager.Instance.Token, new FirstLineIsPathOutputProcessor())
                .Configure(processManager, Environment.IsWindows ? "where" : "which", "git", null, false);
        }
    }

    interface IPlatform
    {
        IProcessEnvironment GitEnvironment { get; }
    }

    class Platform : IPlatform
    {
        public Platform(IEnvironment env, IFileSystem fs)
        {
            GitEnvironment = new ProcessEnvironment(env);
        }
        public IProcessEnvironment GitEnvironment { get; }
    }
}