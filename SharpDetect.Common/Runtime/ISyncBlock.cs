using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Common.Runtime
{
    public interface ISyncBlock
    {
        IShadowThread? LockOwner { get; }
    }
}
