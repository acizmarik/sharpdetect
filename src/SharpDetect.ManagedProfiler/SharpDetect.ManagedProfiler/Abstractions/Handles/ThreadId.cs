namespace SharpDetect.Profiler;

public readonly struct ThreadId
{
    public readonly nuint Value;

    public ThreadId(nuint value)
    {
        Value = value;
    }
}
