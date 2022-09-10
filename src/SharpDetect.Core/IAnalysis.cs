namespace SharpDetect.Core
{
    public interface IAnalysis
    {
        Task<bool> ExecuteOnlyAnalysisAsync(bool dumpStatistics, CancellationToken ct);
        Task<bool> ExecuteAnalysisAndTargetAsync(bool dumpStatistics, CancellationToken ct);
    }
}