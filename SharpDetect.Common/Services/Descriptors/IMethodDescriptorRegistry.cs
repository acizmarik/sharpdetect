using dnlib.DotNet;
using SharpDetect.Common.LibraryDescriptors;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services.Descriptors
{
    public interface IMethodDescriptorRegistry
    {
        void Register(ILibraryDescriptor library);
        void Register((MethodIdentifier Identifier, MethodInterpretationData Interpretation) method, string assemblyName);

        bool TryGetMethodInterpretationData(MethodDef method, [NotNullWhen(returnValue: true)] out MethodInterpretationData? data);

        IEnumerable<string> GetSupportedLibraries();
        ILibraryDescriptor GetCoreLibraryDescriptor();
        IEnumerable<(MethodIdentifier Identifier, MethodInterpretationData Data)> GetRegisteredMethods(string assemblyName);
    }
}
