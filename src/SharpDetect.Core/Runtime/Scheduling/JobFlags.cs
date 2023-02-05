namespace SharpDetect.Core.Runtime.Scheduling
{
    [Flags]
    internal enum JobFlags
    {
        None = 0,
        Concurrent = 1,
        OverrideEpoch = 2,
        OverrideSuspend = 4
    }
}
