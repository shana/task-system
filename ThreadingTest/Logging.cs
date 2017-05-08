using System;
using System.Threading;

namespace GitHub.Unity
{
    class Logging : ILogging
    {
        private Type type;

        internal static ILogging GetLogger(Type type)
        {
            return new Logging(type);
        }

        internal static ILogging GetLogger<T>()
        {
            return new Logging(typeof(T));
        }

        public Logging(Type type)
        {
            this.type = type;
        }

        public void Error(Exception innerException, string msg)
        {
            msg = String.Format("{0} {1}", msg, innerException);
            Console.WriteLine(String.Format("{0}({1}):{2}", type.Name, Thread.CurrentThread.ManagedThreadId, msg));
        }

        public void Debug(string msg, params object[] args)
        {
            if (args != null)
                msg = string.Format(msg, args);
            Console.WriteLine(String.Format("{0}({1}):{2}", type.Name, Thread.CurrentThread.ManagedThreadId, msg));
        }

        public void Trace(string msg, params object[] args)
        {
            if (args != null)
                msg = string.Format(msg, args);
            Console.WriteLine(String.Format("{0}({1}):{2}", type.Name, Thread.CurrentThread.ManagedThreadId, msg));
        }

        public void Error(string msg, params object[] args)
        {
            if (args != null)
                msg = string.Format(msg, args);
            Console.WriteLine(String.Format("{0}({1}):{2}", type.Name, Thread.CurrentThread.ManagedThreadId, msg));
        }
    }
}