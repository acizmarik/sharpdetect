namespace SharpDetect.Core
{
    public interface IAnalysis
    {
        public Task<bool> ExecuteAsync(CancellationToken ct);
    }
}
