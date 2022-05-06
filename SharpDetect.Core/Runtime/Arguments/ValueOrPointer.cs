using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Core.Runtime.Arguments
{
    public struct ValueOrPointer : IValueOrPointer
    {
        public UIntPtr? Pointer { get; }
        public object? BoxedValue { get; }

        public ValueOrPointer(object value)
        {
            Pointer = null;
            BoxedValue = value;
        }

        public ValueOrPointer(UIntPtr ptr)
        {
            BoxedValue = null;
            Pointer = ptr;
        }

        public bool HasValue()
            => BoxedValue is not null;

        public bool HasPointer()
            => Pointer.HasValue;
    }
}
