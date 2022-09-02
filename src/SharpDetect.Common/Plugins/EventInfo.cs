using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Common.Plugins
{
    public record struct EventInfo(IShadowCLR Runtime, IShadowThread Thread);
}
