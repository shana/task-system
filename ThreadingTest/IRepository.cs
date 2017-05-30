using GitHub.Unity;

namespace GitHub.Unity
{
    internal interface IRepository
    {
        NPath LocalPath { get; set; }
    }
}