namespace SharpDetect.Profiler;

public readonly struct ObjectId
{
    public readonly nuint Value;

    public ObjectId(nuint value)
    {
        Value = value;
    }
}
