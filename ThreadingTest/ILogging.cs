using System;

namespace GitHub.Unity
{
    internal interface ILogging
    {
        void Error(Exception innerException, string msg);
        void Error(string msg, params object[] args);
        void Debug(string msg, params object[] args);
        void Trace(string msg, params object[] args);
        void Trace(Exception innerException, string msg);
    }
}