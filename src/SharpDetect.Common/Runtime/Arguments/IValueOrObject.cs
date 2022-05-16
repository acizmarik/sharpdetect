namespace SharpDetect.Common.Runtime.Arguments
{
    public interface IValueOrObject
    {
        bool HasValue();
        bool HasShadowObject();

        object? BoxedValue { get; }
        IShadowObject? ShadowObject { get; }
    }
}
