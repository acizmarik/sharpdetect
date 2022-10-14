using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Common.Diagnostics
{
    public record struct ReportDataEntry(int ProcessId, IShadowThread Thread, AnalysisEventType Type, SourceLink SourceLink);
}
