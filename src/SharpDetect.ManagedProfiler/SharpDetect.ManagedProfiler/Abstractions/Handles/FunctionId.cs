namespace SharpDetect.Profiler;

public readonly struct FunctionId
{
    public readonly nuint Value;

    public FunctionId(nuint value)
    {
        Value = value;
    }
}
