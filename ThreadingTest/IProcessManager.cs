namespace GitHub.Unity
{
    internal interface IProcessManager
    {
        T Configure<T>(T processTask) where T : IProcess;

        T Configure<T>(T processTask, string executableFileName, string arguments, string workingDirectory)
            where T : IProcess;
    }
}