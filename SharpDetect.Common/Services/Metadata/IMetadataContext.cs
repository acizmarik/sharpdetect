namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataContext
    {
        IMetadataEmitter GetEmitter(int processId);
        IMetadataResolver GetResolver(int processId);
    }
}
