using System;

namespace GitHub.Unity
{
    internal interface ILogging
    {
        void Trace(string msg);
        void Error(Exception innerException, string msg);
        void Error(string msg);
    }
}