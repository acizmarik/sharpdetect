using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Diagnostics
{
    public record ErrorReport(string Reporter, string Category, string MessageFormat, object?[]? Arguments, int Pid, SourceLink[]? SourceLinks)
        : ReportBase(Reporter, Category, MessageFormat, Arguments, Pid, SourceLinks);
}
