using System;

namespace GitHub.Unity
{
    internal class Logging : ILogging
    {
        internal static ILogging GetLogger(Type type)
        {
            return new Logging();
        }

        internal static ILogging GetLogger<T>()
        {
            return new Logging();
        }

        public void Error(Exception innerException, string msg)
        {
            Console.WriteLine(String.Format("{0} {1}", msg, innerException.Message));
        }

        public void Trace(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Error(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}