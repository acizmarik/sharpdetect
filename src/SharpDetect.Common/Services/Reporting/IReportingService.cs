using SharpDetect.Common.Diagnostics;

namespace SharpDetect.Common.Services.Reporting
{
    public interface IReportingService
    {
        int ErrorCount { get; }
        int WarningCount { get; }
        int InformationCount { get; }

        void Report(ReportBase report);
    }
}
