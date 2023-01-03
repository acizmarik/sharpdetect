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
    private readonly ThreadLocal<Stack<List<nint>>> indirectsCallStack = new(() => new()); 

    public List<nint> PopIndirects()
    {
        var threadLocal = indirectsCallStack.Value!;
        return threadLocal.Pop();
    }

    public void PushIndirects(List<nint> indirects)
    {
        var threadLocal = indirectsCallStack.Value!;
        threadLocal.Push(indirects);
    }
}
