using dnlib.DotNet;

namespace SharpDetect.Common.Services.Metadata
{
    public interface IMetadataResolversProvider
    {
        AssemblyResolver AssemblyResolver { get; }
        Resolver MemberResolver { get; }
    }
}
