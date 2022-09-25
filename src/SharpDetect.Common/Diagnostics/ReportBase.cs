using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Diagnostics
{
    public abstract record ReportBase(
        string Reporter, 
        string Category, 
        string MessageFormat, 
        object?[]? Arguments, 
        int ProcessId, 
        SourceLink[]? SourceLinks);
}
