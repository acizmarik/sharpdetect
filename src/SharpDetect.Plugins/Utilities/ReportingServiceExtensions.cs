using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Runtime.Threads;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Common.SourceLinks;

namespace SharpDetect.Plugins.Utilities
{
    public static class ReportingServiceExtensions
    {
        public static void CreateReport(
            this IReportingService reporter, 
            string plugin, 
            string messageFormat,
            object[]? arguments,
            string category, 
            int processId, 
            IShadowThread thread,
            SourceLink? link)
        {
            var report = new WarningReport(plugin, category, messageFormat, arguments, processId, thread, link);
            reporter.Report(report);
        }
    }
}
