namespace SharpDetect.Core
{
    public interface IAnalysis
    {
        Task<bool> ExecuteOnlyAnalysisAsync(CancellationToken ct);
        Task<bool> ExecuteAnalysisAndTargetAsync(CancellationToken ct);
    }
}