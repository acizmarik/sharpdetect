using dnlib.DotNet;
using Google.Protobuf;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Unsafe;
using SharpDetect.Profiler;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace SharpDetect.UnitTests.Profiler;

public class MessageFactoryTests
{
    [Fact]
    public void MessageFactoryTests_ProfilerInitialized()
    {
        // Prepare
        var message = MessageFactory.CreateProfilerInitializedNotification();

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.ProfilerInitialized);
    }

    [Fact]
    public void MessageFactoryTests_ProfilerDestroyed()
    {
        // Prepare
        var message = MessageFactory.CreateProfilerDestroyedNotification();

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.ProfilerDestroyed);
    }

    [Fact]
    public void MessageFactoryTests_ModuleLoaded()
    {
        // Prepare
        var modulePath = "module.dll";
        var moduleId = new ModuleId(123);
        var message = MessageFactory.CreateModuleLoadedNotification(moduleId, modulePath);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.ModuleLoaded);
        Assert.Equal(moduleId.Value, message.ModuleLoaded.ModuleId);
        Assert.Equal(modulePath, message.ModuleLoaded.ModulePath);
    }

    [Fact]
    public void MessageFactoryTests_TypeLoaded()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeId = new MdTypeDef(456);
        var message = MessageFactory.CreateTypeLoadedNotification(moduleId, typeId);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.TypeLoaded);
        Assert.Equal(moduleId.Value, message.TypeLoaded.ModuleId);
        Assert.Equal(typeId.Value, (int)message.TypeLoaded.TypeToken);
    }

    [Fact]
    public void MessageFactoryTests_JITCompilationStarted()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeId = new MdTypeDef(456);
        var functionId = new MdMethodDef(789);
        var message = MessageFactory.CreateJITCompilationStartedNotification(moduleId, typeId, functionId);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.JITCompilationStarted);
        Assert.Equal(moduleId.Value, message.JITCompilationStarted.ModuleId);
        Assert.Equal(typeId.Value, (int)message.JITCompilationStarted.TypeToken);
        Assert.Equal(functionId.Value, (int)message.JITCompilationStarted.FunctionToken);
    }


    [Fact]
    public void MessageFactoryTests_ThreadCreated()
    {
        // Prepare
        var threadId = new ThreadId(123);
        var message = MessageFactory.CreateThreadCreatedNotification(threadId);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.ThreadCreated);
        Assert.Equal(threadId.Value, message.ThreadCreated.ThreadId);
    }

    [Fact]
    public void MessageFactoryTests_ThreadDestroyed()
    {
        // Prepare
        var threadId = new ThreadId(123);
        var message = MessageFactory.CreateThreadDestroyedNotification(threadId);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.ThreadDestroyed);
        Assert.Equal(threadId.Value, message.ThreadDestroyed.ThreadId);
    }

    [Fact]
    public void MessageFactoryTests_TypeInjected()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var message = MessageFactory.CreateTypeInjectedNotification(moduleId, typeDef);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.TypeInjected);
        Assert.Equal(moduleId.Value, message.TypeInjected.ModuleId);
        Assert.Equal(typeDef.Value, (int)message.TypeInjected.TypeToken);
    }

    [Fact]
    public void MessageFactoryTests_TypeReferenced()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeRef = new MdTypeRef(456);
        var message = MessageFactory.CreateTypeReferencedNotification(moduleId, typeRef);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.TypeReferenced);
        Assert.Equal(moduleId.Value, message.TypeReferenced.ModuleId);
        Assert.Equal(typeRef.Value, (int)message.TypeReferenced.TypeToken);
    }

    [Fact]
    public void MessageFactoryTests_MethodInjected()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var methodDef = new MdMethodDef(789);
        var type = MethodType.FieldAccess;
        var message = MessageFactory.CreateMethodInjectedNotification(moduleId, typeDef, methodDef, type);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.MethodInjected);
        Assert.Equal(moduleId.Value, message.MethodInjected.ModuleId);
        Assert.Equal(typeDef.Value, (int)message.MethodInjected.TypeToken);
        Assert.Equal(methodDef.Value, (int)message.MethodInjected.FunctionToken);
        Assert.Equal(type, message.MethodInjected.Type);
    }

    [Fact]
    public void MessageFactoryTests_HelperMethodReferenced()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeRef = new MdTypeRef(456);
        var methodRef = new MdMemberRef(789);
        var type = MethodType.FieldAccess;
        var message = MessageFactory.CreateHelperMethodReferencedNotification(moduleId, typeRef, methodRef, type);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.HelperMethodReferenced);
        Assert.Equal(moduleId.Value, message.HelperMethodReferenced.ModuleId);
        Assert.Equal(typeRef.Value, (int)message.HelperMethodReferenced.TypeToken);
        Assert.Equal(methodRef.Value, (int)message.HelperMethodReferenced.FunctionToken);
        Assert.Equal(type, message.HelperMethodReferenced.Type);
    }

    [Fact]
    public void MessageFactoryTests_WrapperMethodReferenced()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var methodDef = new MdMethodDef(789);
        var refModuleId = new ModuleId(321);
        var typeRef = new MdTypeRef(654);
        var methodRef = new MdMemberRef(321);
        var message = MessageFactory.CreateWrapperMethodReferencedNotification(moduleId, typeDef, methodDef, refModuleId, typeRef, methodRef);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.WrapperMethodReferenced);
        Assert.Equal(moduleId.Value, message.WrapperMethodReferenced.DefModuleId);
        Assert.Equal(typeDef.Value, (int)message.WrapperMethodReferenced.DefTypeToken);
        Assert.Equal(methodDef.Value, (int)message.WrapperMethodReferenced.DefFunctionToken);
        Assert.Equal(refModuleId.Value, message.WrapperMethodReferenced.RefModuleId);
        Assert.Equal(typeRef.Value, (int)message.WrapperMethodReferenced.RefTypeToken);
        Assert.Equal(methodRef.Value, (int)message.WrapperMethodReferenced.RefFunctionToken);
    }

    [Fact]
    public void MessageFactoryTests_MethodWrapped()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var nativeMethodDef = new MdMethodDef(789);
        var wrapperMethodDef = new MdMethodDef(987);
        var message = MessageFactory.CreateMethodWrappedNotification(moduleId, typeDef, nativeMethodDef, wrapperMethodDef);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.MethodWrapperInjected);
        Assert.Equal(moduleId.Value, message.MethodWrapperInjected.ModuleId);
        Assert.Equal(typeDef.Value, (int)message.MethodWrapperInjected.TypeToken);
        Assert.Equal(nativeMethodDef.Value, (int)message.MethodWrapperInjected.OriginalFunctionToken);
        Assert.Equal(wrapperMethodDef.Value, (int)message.MethodWrapperInjected.WrapperFunctionToken);
    }

    [Fact]
    public void MessageFactoryTests_MethodCalled()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var methodDef = new MdMethodDef(789);
        var message = MessageFactory.CreateMethodCalledNotification(moduleId, typeDef, methodDef);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.MethodCalled);
        Assert.Equal(moduleId.Value, message.MethodCalled.ModuleId);
        Assert.Equal(typeDef.Value, (int)message.MethodCalled.TypeToken);
        Assert.Equal(methodDef.Value, (int)message.MethodCalled.FunctionToken);
    }

    [Fact]
    public void MessageFactoryTests_MethodCalledWithArguments()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var methodDef = new MdMethodDef(789);
        var argumentValues = new byte[] { 1, 2, 3 };
        var argumentOffsets = new byte[] { 3, 2, 1 };
        var message = MessageFactory.CreateMethodCalledWithArgumentsNotification(moduleId, typeDef, methodDef, argumentValues, argumentOffsets);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.MethodCalled);
        Assert.Equal(moduleId.Value, message.MethodCalled.ModuleId);
        Assert.Equal(typeDef.Value, (int)message.MethodCalled.TypeToken);
        Assert.Equal(methodDef.Value, (int)message.MethodCalled.FunctionToken);
        Assert.Equal(argumentValues, message.MethodCalled.ArgumentValues.ToByteArray());
        Assert.Equal(argumentOffsets, message.MethodCalled.ArgumentOffsets.ToByteArray());
    }

    [Fact]
    public void MessageFactoryTests_MethodReturned()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var methodDef = new MdMethodDef(789);
        var message = MessageFactory.CreateMethodReturnedNotification(moduleId, typeDef, methodDef);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.MethodReturned);
        Assert.Equal(moduleId.Value, message.MethodReturned.ModuleId);
        Assert.Equal(typeDef.Value, (int)message.MethodReturned.TypeToken);
        Assert.Equal(methodDef.Value, (int)message.MethodReturned.FunctionToken);
    }

    [Fact]
    public void MessageFactoryTests_MethodReturnedWithArguments()
    {
        // Prepare
        var moduleId = new ModuleId(123);
        var typeDef = new MdTypeDef(456);
        var methodDef = new MdMethodDef(789);
        var returnValue = new byte[] { 1, 2, 3 };
        var byRefArgumentValues = new byte[] { 4, 5, 6 };
        var byRefArgumentOffsets = new byte[] { 7, 8, 9 };
        var message = MessageFactory.CreateMethodReturnedWithReturnValueNotification(moduleId, typeDef, methodDef, returnValue, byRefArgumentValues, byRefArgumentOffsets);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.MethodReturned);
        Assert.Equal(moduleId.Value, message.MethodReturned.ModuleId);
        Assert.Equal(typeDef.Value, (int)message.MethodReturned.TypeToken);
        Assert.Equal(methodDef.Value, (int)message.MethodReturned.FunctionToken);
        Assert.Equal(returnValue, message.MethodReturned.ReturnValue.ToByteArray());
        Assert.Equal(byRefArgumentValues, message.MethodReturned.ByRefArgumentValues.ToByteArray());
        Assert.Equal(byRefArgumentOffsets, message.MethodReturned.ByRefArgumentOffsets.ToByteArray());
    }

    [Fact]
    public void MessageFactoryTests_GarbageCollectionStarted()
    {
        // Prepare
        var generations = new bool[] { true, false, true };
        var bounds = new COR_PRF_GC_GENERATION_RANGE[]
        {
            new COR_PRF_GC_GENERATION_RANGE(
                COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0,
                new ObjectId(123),
                456,
                789)
        };
        var message = MessageFactory.CreateGarbageCollectionStartedNotification(generations, bounds);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.GarbageCollectionStarted);
        Assert.Equal(generations.ToArray(), MemoryMarshal.Cast<byte, bool>(message.GarbageCollectionStarted.GenerationsCollected.ToByteArray()).ToArray());
        Assert.Equal(bounds.ToArray(), MemoryMarshal.Cast<byte, COR_PRF_GC_GENERATION_RANGE>(message.GarbageCollectionStarted.GenerationSegmentBounds.ToByteArray()).ToArray());
    }

    [Fact]
    public void MessageFactoryTests_GarbageCollectionFinished()
    {
        // Prepare
        var bounds = new COR_PRF_GC_GENERATION_RANGE[]
        {
            new COR_PRF_GC_GENERATION_RANGE(
                COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0,
                new ObjectId(123),
                456,
                789)
        };
        var message = MessageFactory.CreateGarbageCollectionFinishedNotification(bounds);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.GarbageCollectionFinished);
        Assert.Equal(bounds.ToArray(), MemoryMarshal.Cast<byte, COR_PRF_GC_GENERATION_RANGE>(message.GarbageCollectionFinished.GenerationSegmentBounds.ToByteArray()).ToArray());
    }

    [Fact]
    public void MessageFactoryTests_RuntimeSuspendStarted()
    {
        // Prepare
        var reason = COR_PRF_SUSPEND_REASON.COR_PRF_SUSPEND_FOR_GC;
        var message = MessageFactory.CreateRuntimeSuspendStartedNotification(reason);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.RuntimeSuspendStarted);
        Assert.Equal(reason, (COR_PRF_SUSPEND_REASON)message.RuntimeSuspendStarted.Reason);
    }

    [Fact]
    public void MessageFactoryTests_RuntimeSuspendFinished()
    {
        // Prepare
        var message = MessageFactory.CreateRuntimeSuspendFinishedNotification();

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.RuntimeSuspendFinished);
    }

    [Fact]
    public void MessageFactoryTests_RuntimeResumeStarted()
    {
        // Prepare
        var message = MessageFactory.CreateRuntimeResumeStartedNotification();

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.RuntimeResumeStarted);
    }

    [Fact]
    public void MessageFactoryTests_RuntimeResumeFinished()
    {
        // Prepare
        var message = MessageFactory.CreateRuntimeResumeFinishedNotification();

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.RuntimeResumeFinished);
    }

    [Fact]
    public void MessageFactoryTests_RuntimeThreadSuspended()
    {
        // Prepare
        var threadId = new ThreadId(123);
        var message = MessageFactory.CreateRuntimeThreadSuspendedNotification(threadId);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.RuntimeThreadSuspended);
        Assert.Equal(threadId.Value, message.RuntimeThreadSuspended.ThreadId);
    }

    [Fact]
    public void MessageFactoryTests_RuntimeThreadResumed()
    {
        // Prepare
        var threadId = new ThreadId(123);
        var message = MessageFactory.CreateRuntimeThreadResumedNotification(threadId);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.RuntimeThreadResumed);
        Assert.Equal(threadId.Value, message.RuntimeThreadResumed.ThreadId);
    }

    [Fact]
    public void MessageFactoryTests_SurvivingReferences()
    {
        // Prepare
        var blocks = new ObjectId[] { new ObjectId(123), new ObjectId(456) };
        var lengths = new ObjectId[] { new ObjectId(789), new ObjectId(987) };
        var message = MessageFactory.CreateSurvivingReferencesNotification(blocks, lengths);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.SurvivingReferences);
        Assert.Equal(blocks, MemoryMarshal.Cast<byte, ObjectId>(message.SurvivingReferences.Blocks.ToArray()).ToArray());
        Assert.Equal(lengths, MemoryMarshal.Cast<byte, ObjectId>(message.SurvivingReferences.Lengths.ToArray()).ToArray());
    }

    [Fact]
    public void MessageFactoryTests_MovedReferences()
    {
        // Prepare
        var oldBlocks = new ObjectId[] { new ObjectId(123), new ObjectId(456) };
        var newBlocks = new ObjectId[] { new ObjectId(456), new ObjectId(789) };
        var lengths = new ObjectId[] { new ObjectId(987), new ObjectId(654) };
        var message = MessageFactory.CreateMovedReferencesNotification(oldBlocks, newBlocks, lengths);

        // Assert
        AssertCommonMetadata(message);
        Assert.NotNull(message.MovedReferences);
        Assert.Equal(oldBlocks, MemoryMarshal.Cast<byte, ObjectId>(message.MovedReferences.OldBlocks.ToArray()).ToArray());
        Assert.Equal(newBlocks, MemoryMarshal.Cast<byte, ObjectId>(message.MovedReferences.NewBlocks.ToArray()).ToArray());
        Assert.Equal(lengths, MemoryMarshal.Cast<byte, ObjectId>(message.MovedReferences.Lengths.ToArray()).ToArray());
    }

    private static void AssertCommonMetadata(NotifyMessage message)
    {
        Assert.Equal(Environment.ProcessId, message.ProcessId);
        Assert.Equal(Environment.CurrentManagedThreadId, (int)message.ThreadId);
    }
}
