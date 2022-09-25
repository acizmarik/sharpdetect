using SharpDetect.Common.Diagnostics;

namespace SharpDetect.Common.Services.Reporting
{
    public interface IReportsRenderer
    {
        void Render(IEnumerable<ReportBase> reports);
    }
}
