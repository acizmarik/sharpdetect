using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Core.Runtime.Arguments
{
    public struct ValueOrObject : IValueOrObject
    {
        public readonly IShadowObject? ShadowObject { get; }
        public readonly object? BoxedValue { get; }

        public ValueOrObject(object value)
        {
            ShadowObject = null;
            BoxedValue = value;
        }

        public ValueOrObject(IShadowObject? obj)
        {
            BoxedValue = null;
            ShadowObject = obj;
        }

        public bool HasValue()
            => BoxedValue is not null;

        public bool HasShadowObject()
            => ShadowObject is not null || BoxedValue is null;
    }
}
