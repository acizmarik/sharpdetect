namespace SharpDetect.Common.LibraryDescriptors
{
    public interface ILibraryDescriptor
    {
        string AssemblyName { get; }

        IEnumerable<string> GetAssemblyDependencies();
        IEnumerable<(MethodIdentifier, MethodInterpretationData)> GetMethods();
    }
}
