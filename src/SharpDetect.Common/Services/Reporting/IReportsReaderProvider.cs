using SharpDetect.Common.Diagnostics;
using System.Threading.Channels;

namespace SharpDetect.Common.Services.Reporting
{
    public interface IReportsReaderProvider
    {
        ChannelReader<ReportBase> GetReportsReader();
    }
}
