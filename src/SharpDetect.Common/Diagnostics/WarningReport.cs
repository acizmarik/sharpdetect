namespace SharpDetect.Common.Diagnostics
{
    public record WarningReport(string Reporter, string Category, string MessageFormat, object?[]? Arguments, ReportDataEntry[]? Entries)
        : ReportBase(Reporter, Category, MessageFormat, Arguments, Entries);
}
