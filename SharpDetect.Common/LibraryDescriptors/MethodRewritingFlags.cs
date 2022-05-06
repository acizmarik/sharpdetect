namespace SharpDetect.Common.LibraryDescriptors
{
    [Flags]
    public enum MethodRewritingFlags
    {
        None,
        InjectEntryExitHooks,
        CaptureArguments,
        CaptureReturnValue,
        InjectManagedWrapper
    }
}
