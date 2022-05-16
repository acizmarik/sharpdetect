using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Common.Services
{
    public interface IExecutingMessageHub : INotificationsHandler
    {
        event Action<(ulong Identifier, bool IsWrite, EventInfo Info)> FieldAccessed;
        event Action<(UIntPtr Instance, EventInfo Info)> FieldInstanceAccessed;

        event Action<(ulong Identifier, bool IsWrite, EventInfo Info)> ArrayElementAccessed;
        event Action<(UIntPtr Instance, EventInfo Info)> ArrayInstanceAccessed;
        event Action<(int Index, EventInfo Info)> ArrayIndexAccessed;

        event Action<(FunctionInfo Function, (ushort, IValueOrPointer)[]? Arguments, EventInfo Info)> MethodCalled;
        event Action<(FunctionInfo Function, IValueOrPointer? ReturnValue, (ushort, IValueOrPointer)[]? ByRefArguments, EventInfo Info)> MethodReturned;

        event Action<(FunctionInfo Function, UIntPtr Instance, EventInfo Info)> LockAcquireAttempted;
        event Action<(FunctionInfo Function, bool IsSuccess, EventInfo Info)> LockAcquireReturned;
        event Action<(FunctionInfo Function, UIntPtr Instance, EventInfo Info)> LockReleaseCalled;
        event Action<(FunctionInfo Function, EventInfo Info)> LockReleaseReturned;

        event Action<(FunctionInfo Function, UIntPtr Instance, EventInfo Info)> ObjectWaitAttempted;
        event Action<(FunctionInfo Function, bool IsSuccess, EventInfo Info)> ObjectWaitReturned;
        event Action<(FunctionInfo Function, bool IsPulseAll, UIntPtr Instance, EventInfo Info)> ObjectPulseCalled;
        event Action<(FunctionInfo Function, bool IsPulseAll, EventInfo Info)> ObjectPulseReturned;
    }
}
