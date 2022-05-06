namespace SharpDetect.Common.Runtime.Threads
{
    public interface IShadowThread
    {
        UIntPtr Id { get; }
        string DisplayName { get; }
        ShadowThreadState State { get; }
    }
}
