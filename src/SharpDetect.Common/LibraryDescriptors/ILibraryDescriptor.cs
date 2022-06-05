namespace SharpDetect.Common.LibraryDescriptors
{
    public interface ILibraryDescriptor
    {
        bool IsCoreLibrary { get; }
        string AssemblyName { get; }
        IReadOnlyList<(MethodIdentifier Identifier, MethodInterpretationData Data)> Methods { get; }
    }
}
