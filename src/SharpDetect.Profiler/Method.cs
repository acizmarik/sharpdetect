namespace SharpDetect.Profiler;

internal record Method(
    ModuleId ModuleId, 
    MdTypeDef TypeDef, 
    MdMethodDef MethodDef, 
    List<(ushort, ushort, bool)> ArgumentInfos,
    ulong TotalArgumentValuesSize,
    ulong TotalIndirectArgumentValuesSize, 
    bool CaptureArguments, 
    bool CaptureReturnValue)
{
    private readonly Stack<List<nint>> indirectsCallstack = new();

    public List<nint> PopIndirects() => indirectsCallstack.Pop();
    public void PushIndirects(List<nint> indirects) => indirectsCallstack.Push(indirects);
}
