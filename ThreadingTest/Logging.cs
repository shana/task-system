using System;

namespace GitHub.Unity
{
    internal class Logging : ILogging
    {
        internal static ILogging GetLogger(Type type)
        {
            return null;
        }

        internal static ILogging GetLogger<T>()
        {
            return null;
        }

        public void Error(string v, string executable, string directory, Exception e)
        {
        }

        public void Trace(string v)
        {
        }
    }
}