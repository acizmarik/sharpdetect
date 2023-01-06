using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpDetect.Common.Exceptions
{
    public static class RuntimeContract
    {
        [DebuggerHidden]
        [StackTraceHidden]
        public static void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression("condition")] string? expr = null)
        {
            if (!condition)
                throw new ShadowRuntimeStateException(expr ?? "Shadow runtime contract broken");
        }
    }
}
