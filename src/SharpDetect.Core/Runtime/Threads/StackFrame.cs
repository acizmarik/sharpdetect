using SharpDetect.Common;

namespace SharpDetect.Core.Runtime.Threads
{
    internal record struct StackFrame(FunctionInfo FunctionInfo, MethodInterpretation Interpretation, object? Arguments);
}
