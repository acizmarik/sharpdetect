namespace SharpDetect.Common.LibraryDescriptors
{
    public interface ILibraryDescriptor
    {
        bool IsCoreLibrary { get; }
        string AssemblyName { get; }

        IEnumerable<string> GetAssemblyDependencies();
        IEnumerable<(MethodIdentifier Identifier, MethodInterpretationData Data)> GetMethods();
    }
}
