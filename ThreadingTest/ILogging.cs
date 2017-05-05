using System;

namespace GitHub.Unity
{
    internal interface ILogging
    {
        void Trace(string v);
        void Error(string v, string executable, string directory, Exception e);
    }
}