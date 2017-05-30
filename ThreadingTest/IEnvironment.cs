using System;

namespace GitHub.Unity
{
    internal interface IEnvironment
    {
        bool IsMac { get; set; }
        bool IsWindows { get; set; }
        string GitExecutablePath { get; set; }
        string RepositoryPath { get; }
        string GetEnvironmentVariable(string v);
        string ExpandEnvironmentVariables(string unquoted);
    }

    class DefaultEnvironment : IEnvironment
    {
        public bool IsMac { get { return false; } set { } }

        public bool IsWindows { get { return true; } set { } }

        public string ExpandEnvironmentVariables(string unquoted)
        {
            return unquoted;
        }

        public string GetEnvironmentVariable(string v)
        {
            return "";
        }
        public string GitExecutablePath { get { return "git"; } set { } }
        public IRepository Repository { get; set; }
        public string RepositoryPath { get { return Repository.LocalPath; } }
    }
}