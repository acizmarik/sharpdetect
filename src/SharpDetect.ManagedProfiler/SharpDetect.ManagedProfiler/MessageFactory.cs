using Google.Protobuf;
using SharpDetect.Common.Messages;
using System;
using System.Runtime.InteropServices;
using MethodType = SharpDetect.Common.Messages.MethodType;

namespace SharpDetect.Profiler
{
    internal static class MessageFactory
    {
        public static NotifyMessage CreateProfilerInitializedNotification()
        {
            var message = new NotifyMessage() { ProfilerInitialized = new Notify_ProfilerInitialized() };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateProfilerDestroyedNotification()
        {
            var message = new NotifyMessage() { ProfilerDestroyed = new Notify_ProfilerDestroyed() };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateModuleLoadedNotification(ModuleId moduleId, string path)
        {
            var message = new NotifyMessage() { ModuleLoaded = new Notify_ModuleLoaded()
            {
                ModuleId = moduleId.Value,
                ModulePath = path
            }};
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateTypeLoadedNotification(ModuleId moduleId, MdTypeDef typeDef)
        {
            var message = new NotifyMessage()
            {
                TypeLoaded = new Notify_TypeLoaded()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateJITCompilationStartedNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef methodDef)
        {
            var message = new NotifyMessage()
            {
                JITCompilationStarted = new Notify_JITCompilationStarted()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value,
                    FunctionToken = (uint)methodDef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateThreadCreatedNotification(ThreadId threadId)
        {
            var message = new NotifyMessage()
            {
                ThreadCreated = new Notify_ThreadCreated()
                {
                    ThreadId = threadId.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateThreadDestroyedNotification(ThreadId threadId)
        {
            var message = new NotifyMessage()
            {
                ThreadDestroyed = new Notify_ThreadDestroyed()
                {
                    ThreadId = threadId.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateTypeInjectedNotification(ModuleId moduleId, MdTypeDef typeDef)
        {
            var message = new NotifyMessage()
            {
                TypeInjected = new Notify_TypeInjected()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateTypeReferencedNotification(ModuleId moduleId, MdTypeRef typeRef)
        {
            var message = new NotifyMessage()
            {
                TypeReferenced = new Notify_TypeReferenced()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeRef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateMethodInjectedNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef methodDef, MethodType type)
        {
            var message = new NotifyMessage()
            {
                MethodInjected = new Notify_MethodInjected()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value,
                    FunctionToken = (uint)methodDef.Value,
                    Type = type
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateHelperMethodReferencedNotification(ModuleId moduleId, MdTypeRef typeRef, MdMemberRef methodRef, MethodType type)
        {
            var message = new NotifyMessage()
            {
                HelperMethodReferenced = new Notify_HelperMethodReferenced()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeRef.Value,
                    FunctionToken = (uint)methodRef.Value,
                    Type = type
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateWrapperMethodReferencedNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef methodDef, ModuleId refModuleId, MdTypeRef typeRef, MdMemberRef methodRef)
        {
            var message = new NotifyMessage()
            {
                WrapperMethodReferenced = new Notify_WrapperMethodReferenced()
                {
                    DefModuleId = moduleId.Value,
                    DefTypeToken = (uint)typeDef.Value,
                    DefFunctionToken = (uint)methodDef.Value,
                    RefModuleId = refModuleId.Value,
                    RefTypeToken = (uint)typeRef.Value,
                    RefFunctionToken = (uint)methodRef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateMethodWrappedNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef nativeMethodDef, MdMethodDef wrapperMethodDef)
        {
            var message = new NotifyMessage()
            {
                MethodWrapperInjected = new Notify_MethodWrapperInjected()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value,
                    OriginalFunctionToken = (uint)nativeMethodDef.Value,
                    WrapperFunctionToken = (uint)wrapperMethodDef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateMethodCalledNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef methodDef)
        {
            var message = new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value,
                    FunctionToken = (uint)methodDef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateMethodCalledWithArgumentsNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef methodDef, ReadOnlySpan<byte> argValues, ReadOnlySpan<byte> argOffsets)
        {
            var message = new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value,
                    FunctionToken = (uint)methodDef.Value,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateMethodReturnedNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef methodDef)
        {
            var message = new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value,
                    FunctionToken = (uint)methodDef.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateMethodReturnedWithReturnValueNotification(ModuleId moduleId, MdTypeDef typeDef, MdMethodDef methodDef, ReadOnlySpan<byte> returnValue, ReadOnlySpan<byte> byRefArgValues, ReadOnlySpan<byte> byRefArgOffsets)
        {
            var message = new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = moduleId.Value,
                    TypeToken = (uint)typeDef.Value,
                    FunctionToken = (uint)methodDef.Value,
                    ReturnValue = ByteString.CopyFrom(returnValue),
                    ByRefArgumentValues = ByteString.CopyFrom(byRefArgValues),
                    ByRefArgumentOffsets = ByteString.CopyFrom(byRefArgOffsets)
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateGarbageCollectionStartedNotification(ReadOnlySpan<bool> generations, ReadOnlySpan<COR_PRF_GC_GENERATION_RANGE> bounds)
        {
            var generationBytes = MemoryMarshal.Cast<bool, byte>(generations);
            var boundBytes = MemoryMarshal.Cast<COR_PRF_GC_GENERATION_RANGE, byte>(bounds);

            var message = new NotifyMessage()
            {
                GarbageCollectionStarted = new Notify_GarbageCollectionStarted()
                {
                    GenerationsCollected = ByteString.CopyFrom(generationBytes),
                    GenerationSegmentBounds = ByteString.CopyFrom(boundBytes)
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateGarbageCollectionFinishedNotification(ReadOnlySpan<COR_PRF_GC_GENERATION_RANGE> bounds)
        {
            var boundBytes = MemoryMarshal.Cast<COR_PRF_GC_GENERATION_RANGE, byte>(bounds);

            var message = new NotifyMessage()
            {
                GarbageCollectionFinished = new Notify_GarbageCollectionFinished()
                {
                    GenerationSegmentBounds = ByteString.CopyFrom(boundBytes)
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateRuntimeSuspendStartedNotification(COR_PRF_SUSPEND_REASON reason)
        {
            var message = new NotifyMessage()
            {
                RuntimeSuspendStarted = new Notify_RuntimeSuspendStarted()
                {
                    Reason = (SUSPEND_REASON)reason
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateRuntimeSuspendFinishedNotification()
        {
            var message = new NotifyMessage() { RuntimeSuspendFinished = new Notify_RuntimeSuspendFinished() };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateRuntimeResumeStartedNotification()
        {
            var message = new NotifyMessage() { RuntimeResumeStarted = new Notify_RuntimeResumeStarted() };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateRuntimeResumeFinishedNotification()
        {
            var message = new NotifyMessage() { RuntimeResumeFinished = new Notify_RuntimeResumeFinished() };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateRuntimeThreadSuspendedNotification(ThreadId threadId)
        {
            var message = new NotifyMessage()
            {
                RuntimeThreadSuspended = new Notify_RuntimeThreadSuspended()
                {
                    ThreadId = threadId.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateRuntimeThreadResumedNotification(ThreadId threadId)
        {
            var message = new NotifyMessage()
            {
                RuntimeThreadResumed = new Notify_RuntimeThreadResumed()
                {
                    ThreadId = threadId.Value
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateSurvivingReferencesNotification(ReadOnlySpan<ObjectId> ranges, ReadOnlySpan<ObjectId> lengths)
        {
            var rangesBytes = MemoryMarshal.Cast<ObjectId, byte>(ranges);
            var lengthsBytes = MemoryMarshal.Cast<ObjectId, byte>(lengths);

            var message = new NotifyMessage()
            {
                SurvivingReferences = new Notify_SurvivingReferences()
                {
                    Blocks = ByteString.CopyFrom(rangesBytes),
                    Lengths = ByteString.CopyFrom(lengthsBytes)
                }
            };
            return AddCommonMetadata(message);
        }

        public static NotifyMessage CreateMovedReferencesNotification(ReadOnlySpan<ObjectId> oldRanges, ReadOnlySpan<ObjectId> newRanges, ReadOnlySpan<ObjectId> lengths)
        {
            var oldRangesBytes = MemoryMarshal.Cast<ObjectId, byte>(oldRanges);
            var newRangesBytes = MemoryMarshal.Cast<ObjectId, byte>(newRanges);
            var lengthsBytes = MemoryMarshal.Cast<ObjectId, byte>(lengths);

            var message = new NotifyMessage()
            {
                MovedReferences = new Notify_MovedReferences()
                {
                    OldBlocks = ByteString.CopyFrom(oldRangesBytes),
                    NewBlocks = ByteString.CopyFrom(newRangesBytes),
                    Lengths = ByteString.CopyFrom(lengthsBytes)
                }
            };
            return AddCommonMetadata(message);
        }

        private static NotifyMessage AddCommonMetadata(NotifyMessage message)
        {
            message.ProcessId = Environment.ProcessId;
            message.ThreadId = (ulong)Environment.CurrentManagedThreadId;
            return message;
        }
    }
}
