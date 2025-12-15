// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.Descriptors;

internal static class MonitorMethodDescriptors
{
    private const string MonitorTypeName = "System.Threading.Monitor";
    private static readonly Version Version8 = new(8, 0, 0);
    private static readonly Version Version9Max = new(9, int.MaxValue, int.MaxValue);
    private static readonly Version Version10 = new(10, 0, 0);
    private static readonly Version Version10Max = new(10, int.MaxValue, int.MaxValue);
    private static readonly CapturedArgumentDescriptor ObjectRefArg = 
        new(0, new((byte)nint.Size, CapturedValue.CaptureAsReference));
    private static readonly CapturedArgumentDescriptor BoolRefArg = 
        new(1, new(sizeof(bool), CapturedValue.CaptureAsValue | CapturedValue.IndirectLoad));
    private static readonly CapturedArgumentDescriptor BoolRefArgAt2 = 
        new(2, new(sizeof(bool), CapturedValue.CaptureAsValue | CapturedValue.IndirectLoad));
    private static readonly CapturedValueDescriptor BoolReturnValue = 
        new(sizeof(bool), CapturedValue.CaptureAsValue);
    
    // Enter methods
    private static readonly MethodDescriptor MonitorEnterObjectV8;
    private static readonly MethodDescriptor MonitorEnterObjectV10;
    private static readonly MethodDescriptor MonitorEnterObjectLockTakenV10;
    private static readonly MethodDescriptor MonitorReliableEnterV8;
    private static readonly MethodDescriptor MonitorReliableEnterTimeoutV8;
    
    // TryEnter methods
    private static readonly MethodDescriptor MonitorTryEnterObjectV10;
    private static readonly MethodDescriptor MonitorTryEnterTimeoutObjectV10;
    private static readonly MethodDescriptor MonitorTryReliableEnterObjectV10;
    private static readonly MethodDescriptor MonitorTryReliableEnterObjectTimeoutV10;
    
    // Exit methods
    private static readonly MethodDescriptor MonitorExitV8;
    private static readonly MethodDescriptor MonitorExitV10;
    
    // Signal methods
    private static readonly MethodDescriptor MonitorPulseOne;
    private static readonly MethodDescriptor MonitorPulseAll;
    
    // Wait methods
    private static readonly MethodDescriptor MonitorWaitObject;
    private static readonly MethodDescriptor MonitorWaitObjectInt;

    static MonitorMethodDescriptors()
    {
        // Initialize Enter methods
        MonitorEnterObjectV8 = CreateEnterDescriptor_v8();
        MonitorEnterObjectV10 = CreateEnterDescriptor_v10();
        MonitorEnterObjectLockTakenV10 = CreateEnterWithLockTakenDescriptor_v10();
        MonitorReliableEnterV8 = CreateReliableEnterDescriptor_v8();
        MonitorReliableEnterTimeoutV8 = CreateReliableEnterTimeoutDescriptor_v8();
        
        // Initialize TryEnter methods
        MonitorTryEnterObjectV10 = CreateTryEnterDescriptor_v10();
        MonitorTryEnterTimeoutObjectV10 = CreateTryEnterTimeoutDescriptor_v10();
        MonitorTryReliableEnterObjectV10 = CreateTryReliableEnterDescriptor_v10();
        MonitorTryReliableEnterObjectTimeoutV10 = CreateTryReliableEnterTimeoutDescriptor_v10();
        
        // Initialize Exit methods
        MonitorExitV8 = CreateExitDescriptor_v8();
        MonitorExitV10 = CreateExitDescriptor_v10();
        
        // Initialize Signal methods
        MonitorPulseOne = CreatePulseDescriptor();
        MonitorPulseAll = CreatePulseAllDescriptor();
        
        // Initialize Wait methods
        MonitorWaitObject = CreateWaitDescriptor();
        MonitorWaitObjectInt = CreateWaitTimeoutDescriptor();
    }

    #region Helper Methods for Creating Descriptors

    // Enter method descriptors
    private static MethodDescriptor CreateEnterDescriptor_v8() => new(
        "Enter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version8, Version9Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: true,
            arguments: [ObjectRefArg],
            returnValue: null,
            RecordedEventType.MonitorLockAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    private static MethodDescriptor CreateEnterDescriptor_v10() => new(
        "Enter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version10, Version10Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: null,
            RecordedEventType.MonitorLockAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    private static MethodDescriptor CreateEnterWithLockTakenDescriptor_v10() => new(
        "Enter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version10, Version10Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
            ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))),
        CreateRewritingDescriptor(
            injectManagedWrapper: true,
            arguments: [ObjectRefArg, BoolRefArg],
            returnValue: null,
            RecordedEventType.MonitorLockAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    private static MethodDescriptor CreateReliableEnterDescriptor_v8() => new(
        "ReliableEnter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version8, Version9Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
            ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))),
        CreateRewritingDescriptor(
            injectManagedWrapper: true,
            arguments: [ObjectRefArg, BoolRefArg],
            returnValue: null,
            RecordedEventType.MonitorLockAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    private static MethodDescriptor CreateReliableEnterTimeoutDescriptor_v8() => new(
        "ReliableEnterTimeout",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version8, Version9Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
            ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))),
        CreateRewritingDescriptor(
            injectManagedWrapper: true,
            arguments: [ObjectRefArg, BoolRefArgAt2],
            returnValue: null,
            RecordedEventType.MonitorLockTryAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    // TryEnter method descriptors
    private static MethodDescriptor CreateTryEnterDescriptor_v10() => new(
        "TryEnter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version10, Version10Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_BOOLEAN,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: BoolReturnValue,
            RecordedEventType.MonitorLockTryAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    private static MethodDescriptor CreateTryEnterTimeoutDescriptor_v10() => new(
        "TryEnter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version10, Version10Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_BOOLEAN,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: BoolReturnValue,
            RecordedEventType.MonitorLockTryAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    private static MethodDescriptor CreateTryReliableEnterDescriptor_v10() => new(
        "TryEnter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version10, Version10Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
            ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg, BoolRefArg],
            returnValue: null,
            RecordedEventType.MonitorLockTryAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    private static MethodDescriptor CreateTryReliableEnterTimeoutDescriptor_v10() => new(
        "TryEnter",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version10, Version10Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4),
            ArgumentTypeDescriptor.CreateByRef(ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_BOOLEAN))),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg, BoolRefArgAt2],
            returnValue: null,
            RecordedEventType.MonitorLockTryAcquire,
            RecordedEventType.MonitorLockAcquireResult));

    // Exit method descriptors
    private static MethodDescriptor CreateExitDescriptor_v8() => new(
        "Exit",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version8, Version9Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: true,
            arguments: [ObjectRefArg],
            returnValue: null,
            RecordedEventType.MonitorLockRelease,
            RecordedEventType.MonitorLockReleaseResult));

    private static MethodDescriptor CreateExitDescriptor_v10() => new(
        "Exit",
        MonitorTypeName,
        MethodVersionDescriptor.Create(Version10, Version10Max),
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: null,
            RecordedEventType.MonitorLockRelease,
            RecordedEventType.MonitorLockReleaseResult));

    // Signal method descriptors
    private static MethodDescriptor CreatePulseDescriptor() => new(
        "Pulse",
        MonitorTypeName,
        VersionDescriptor: null,
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: null,
            RecordedEventType.MonitorPulseOneAttempt,
            RecordedEventType.MonitorPulseOneResult));

    private static MethodDescriptor CreatePulseAllDescriptor() => new(
        "PulseAll",
        MonitorTypeName,
        VersionDescriptor: null,
        CreateSignature(
            CorElementType.ELEMENT_TYPE_VOID,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: null,
            RecordedEventType.MonitorPulseAllAttempt,
            RecordedEventType.MonitorPulseAllResult));

    // Wait method descriptors
    private static MethodDescriptor CreateWaitDescriptor() => new(
        "Wait",
        MonitorTypeName,
        VersionDescriptor: null,
        CreateSignature(
            CorElementType.ELEMENT_TYPE_BOOLEAN,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: BoolReturnValue,
            RecordedEventType.MonitorWaitAttempt,
            RecordedEventType.MonitorWaitResult));

    private static MethodDescriptor CreateWaitTimeoutDescriptor() => new(
        "Wait",
        MonitorTypeName,
        VersionDescriptor: null,
        CreateSignature(
            CorElementType.ELEMENT_TYPE_BOOLEAN,
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_OBJECT),
            ArgumentTypeDescriptor.CreateSimple(CorElementType.ELEMENT_TYPE_I4)),
        CreateRewritingDescriptor(
            injectManagedWrapper: false,
            arguments: [ObjectRefArg],
            returnValue: BoolReturnValue,
            RecordedEventType.MonitorWaitAttempt,
            RecordedEventType.MonitorWaitResult));

    // Common helper methods
    private static MethodSignatureDescriptor CreateSignature(
        CorElementType returnType,
        params ArgumentTypeDescriptor[] arguments) => new(
            CorCallingConvention.IMAGE_CEE_CS_CALLCONV_DEFAULT,
            ParametersCount: (byte)arguments.Length,
            ArgumentTypeDescriptor.CreateSimple(returnType),
            arguments);

    private static MethodRewritingDescriptor CreateRewritingDescriptor(
        bool injectManagedWrapper,
        CapturedArgumentDescriptor[] arguments,
        CapturedValueDescriptor? returnValue,
        RecordedEventType beforeEventType,
        RecordedEventType afterEventType) => new(
            InjectHooks: true,
            InjectManagedWrapper: injectManagedWrapper,
            Arguments: arguments,
            ReturnValue: returnValue,
            (ushort)beforeEventType,
            (ushort)afterEventType);

    #endregion

    public static IEnumerable<MethodDescriptor> GetAllMethods()
    {
        return GetAllMethodsV8().Concat(
            GetAllMethodsV10()).Concat(
            GetAllMethodsSdkAgnostic());
    }

    private static IEnumerable<MethodDescriptor> GetAllMethodsV8()
    {
        // Common public API
        yield return MonitorEnterObjectV8;
        yield return MonitorExitV8;
        
        // Internal API (usable only by BCL)
        yield return MonitorReliableEnterV8;
        yield return MonitorReliableEnterTimeoutV8;
    }

    private static IEnumerable<MethodDescriptor> GetAllMethodsV10()
    {
        // Common public API
        yield return MonitorEnterObjectV10;
        yield return MonitorEnterObjectLockTakenV10;
        yield return MonitorTryEnterObjectV10;
        yield return MonitorTryEnterTimeoutObjectV10;
        yield return MonitorTryReliableEnterObjectV10;
        yield return MonitorTryReliableEnterObjectTimeoutV10;
        yield return MonitorExitV10;
    }

    private static IEnumerable<MethodDescriptor> GetAllMethodsSdkAgnostic()
    {
        // Common public API
        yield return MonitorPulseOne;
        yield return MonitorPulseAll;
        yield return MonitorWaitObject;
        yield return MonitorWaitObjectInt;
    }
}

