using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Services.Reporting;
using System.Threading.Channels;

namespace SharpDetect.Core.Reporting
{
    internal class ReportingService : IReportingService, IReportsReader, IDisposable
    {
        public int ErrorCount { get { return errorCount; } }
        public int WarningCount { get { return warningCount; } }
        public int InformationCount { get { return informationCount; } }
        private volatile int errorCount, warningCount, informationCount;
        private readonly Channel<ReportBase> reports;
        private bool isDisposed;

        public ReportingService()
        {
            this.reports = Channel.CreateUnbounded<ReportBase>();
        }

        public ChannelReader<ReportBase> GetReportsReader()
        {
            return reports.Reader;
        }

        public void Report(ReportBase report)
        {
            switch (report)
            {
                case ErrorReport:
                    errorCount++;
                    break;
                case WarningReport:
                    warningCount++;
                    break;
                case InformationReport:
                    informationCount++;
                    break;
            }

            Task.Run(() => reports.Writer.WriteAsync(report)).Wait();
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                reports.Writer.Complete();
                GC.SuppressFinalize(this);
            }
        }
    }
}
