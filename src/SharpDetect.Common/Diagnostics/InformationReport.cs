namespace SharpDetect.Common.Diagnostics
{
    public record InformationReport(string Reporter, string Category, string MessageFormat, object?[]? Arguments, ReportDataEntry[]? Entries)
        : ReportBase(Reporter, Category, MessageFormat, Arguments, Entries);
}
