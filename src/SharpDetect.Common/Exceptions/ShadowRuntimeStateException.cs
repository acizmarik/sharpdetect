using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Exceptions
{
    public class ShadowRuntimeStateException : Exception
    {
        public ShadowRuntimeStateException(string message)
            : base(message)
        {

        }

        [DoesNotReturn]
        public static void Throw(string message)
            => throw new ShadowRuntimeStateException(message);
    }
}
