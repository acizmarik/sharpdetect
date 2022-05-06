using dnlib.DotNet;

namespace SharpDetect.Common
{
    public record struct EventInfo(ulong Id, int ProcessId, UIntPtr ThreadId);
}
