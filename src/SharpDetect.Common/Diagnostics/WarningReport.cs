using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Diagnostics
{
    public record WarningReport(string Reporter, string Category, string Description, int Pid, SourceLink[]? SourceLinks)
        : ReportBase(Reporter, Category, Description, Pid, SourceLinks);
}
