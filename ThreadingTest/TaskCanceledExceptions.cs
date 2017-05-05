using System;
using System.Threading.Tasks;

namespace ThreadingTest
{
    class DependentTaskFailedException : TaskCanceledException
    {
        public DependentTaskFailedException(ITask task, Exception ex) : base(ex.InnerException != null ? ex.InnerException.Message : ex.Message, ex.InnerException ?? ex)
        {}
    }

    class ProcessException : TaskCanceledException
    {
        public ProcessException(ITask process) : base(process.Errors)
        { }
    }
}