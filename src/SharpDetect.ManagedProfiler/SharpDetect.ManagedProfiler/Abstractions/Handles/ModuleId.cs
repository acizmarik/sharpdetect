namespace SharpDetect.Profiler;

public readonly struct ModuleId
{
    public readonly nuint Value;

    public ModuleId(nuint value)
    {
        Value = value;
    }
}
