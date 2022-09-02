using SharpDetect.Common.Interop;

namespace SharpDetect.Common.Runtime
{
    public interface IShadowCLR
    {
        int ProcessId { get; }
        ShadowRuntimeState State { get; }
        COR_PRF_SUSPEND_REASON? SuspensionReason { get; }
    }
}
