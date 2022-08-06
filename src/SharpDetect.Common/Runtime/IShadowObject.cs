namespace SharpDetect.Common.Runtime
{
    public interface IShadowObject : IEquatable<IShadowObject>
    {
        bool IsAlive { get; }
        UIntPtr ShadowPointer { get; }
        ISyncBlock SyncBlock { get; }
    }
}
