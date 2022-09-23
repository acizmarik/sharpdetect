using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Plugins.Utilities
{
    public static class ReportingServiceExtensions
    {
        public static void CreateReport(
            this IReportingService reporter, 
            string plugin, 
            string message, 
            string category, 
            int processId, 
            params SourceLink[] links)
        {
            var report = new WarningReport(plugin, category, message, processId, links);
            reporter.Report(report);
        }
    }
}
