using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Diagnostics
{
    public record ErrorReport(string Reporter, string Category, string MessageFormat, object?[]? Arguments, int Pid, IShadowThread Thread, SourceLink SourceLink)
        : ReportBase(Reporter, Category, MessageFormat, Arguments, Pid, Thread, SourceLink);
}
