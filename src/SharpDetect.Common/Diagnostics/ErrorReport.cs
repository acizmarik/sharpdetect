namespace SharpDetect.Common.Diagnostics
{
    public record ErrorReport(string Reporter, string Category, string MessageFormat, object?[]? Arguments, ReportDataEntry[]? Entries)
        : ReportBase(Reporter, Category, MessageFormat, Arguments, Entries);
}
