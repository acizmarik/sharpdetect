namespace SharpDetect.Common.Runtime.Arguments
{
    public interface IValueOrPointer
    {
        bool HasValue();
        bool HasPointer();

        object? BoxedValue { get; }
        UIntPtr? Pointer { get; }
    }
}
