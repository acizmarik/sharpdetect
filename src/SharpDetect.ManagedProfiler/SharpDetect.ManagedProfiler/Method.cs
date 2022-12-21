namespace SharpDetect.Profiler
{
    internal record Method(
        ModuleId ModuleId, 
        MdTypeDef TypeDef, 
        MdMethodDef MethodDef, 
        List<(ushort, ushort, bool)> ArgumentInfos,
        ulong TotalArgumentValuesSize,
        ulong TotalIndirectArgumentValuesSize);
}
