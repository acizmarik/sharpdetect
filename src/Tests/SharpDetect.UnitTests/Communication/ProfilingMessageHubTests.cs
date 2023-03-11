// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Google.Protobuf;
using SharpDetect.Common;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Core.Communication;
using System.Runtime.InteropServices;
using Xunit;

namespace SharpDetect.UnitTests.Communication
{
    public class ProfilingMessageHubTests : TestsBase
    {
        [Fact]
        public void ProfilingMessageHub_Heartbeat()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);

            var raised = false;
            var info = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.Heartbeat += args =>
            {
                info = args;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage() 
            { 
                Heartbeat = new Notify_Heartbeat(), 
                ProcessId = processId, 
                ThreadId = threadId.ToUInt64(), 
                NotificationId = notificationId 
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
            Assert.Equal(threadId, info.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_ProfilerInitialized()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);

            var raised = false;
            var version = default(Version?);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.ProfilerInitialized += args =>
            {
                version = args.Version;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_ProfilerDestroyed()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);

            var raised = false;
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.ProfilerDestroyed += args =>
            {
                eventInfo = args;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                ProfilerDestroyed = new Notify_ProfilerDestroyed(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_ModuleLoaded()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(321);
            string modulePath = "/assemblies/assembly.dll";

            var raised = false;
            var moduleInfo = default(UIntPtr);
            var pathInfo = default(string);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.ModuleLoaded += args =>
            {
                moduleInfo = args.ModuleId;
                pathInfo = args.Path;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded() { ModuleId = moduleId.ToUInt64(), ModulePath = modulePath },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, moduleInfo);
            Assert.Equal(modulePath, pathInfo);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_TypeLoaded()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(321);
            MDToken typeId = new(654);

            var raised = false;
            var typeInfo = default(TypeInfo);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.TypeLoaded += args =>
            {
                typeInfo = args.TypeInfo;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                TypeLoaded = new Notify_TypeLoaded() { ModuleId = moduleId.ToUInt64(), TypeToken = typeId.Raw },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, typeInfo.ModuleId);
            Assert.Equal(typeId, typeInfo.TypeToken);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_JITCompilationStarted()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(321);
            MDToken typeId = new(654);
            MDToken functionId = new(987);

            var raised = false;
            var functionInfo = default(FunctionInfo);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.JITCompilationStarted += args =>
            {
                functionInfo = args.FunctionInfo;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                JITCompilationStarted = new Notify_JITCompilationStarted() { ModuleId = moduleId.ToUInt64(), TypeToken = typeId.Raw, FunctionToken = functionId.Raw },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, functionInfo.ModuleId);
            Assert.Equal(typeId, functionInfo.TypeToken);
            Assert.Equal(functionId, functionInfo.FunctionToken);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_ThreadCreated()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr newThreadId = new(321);

            var raised = false;
            var threadInfo = default(UIntPtr);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.ThreadCreated += args =>
            {
                threadInfo = args.ThreadId;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                ThreadCreated = new Notify_ThreadCreated() { ThreadId = newThreadId.ToUInt64() },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(newThreadId, threadInfo);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_ThreadDestroyed()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr destroyedThreadId = new(321);

            var raised = false;
            var threadInfo = default(UIntPtr);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.ThreadDestroyed += args =>
            {
                threadInfo = args.ThreadId;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                ThreadDestroyed = new Notify_ThreadDestroyed() { ThreadId = destroyedThreadId.ToUInt64() },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(destroyedThreadId, threadInfo);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_RuntimeSuspendStarted()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            const COR_PRF_SUSPEND_REASON reason = COR_PRF_SUSPEND_REASON.GC;

            var raised = false;
            var reasonInfo = default(COR_PRF_SUSPEND_REASON);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.RuntimeSuspendStarted += args =>
            {
                reasonInfo = args.Reason;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                RuntimeSuspendStarted = new Notify_RuntimeSuspendStarted() { Reason = SUSPEND_REASON.Gc },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(reason, reasonInfo);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_RuntimeSuspend()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);

            var raised = false;
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.RuntimeSuspendFinished += args =>
            {
                eventInfo = args;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                RuntimeSuspendFinished = new Notify_RuntimeSuspendFinished() { },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_RuntimeThreadSuspended()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr suspendedThreadId = new(321);

            var raised = false;
            var threadInfo = default(UIntPtr);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.RuntimeThreadSuspended += args =>
            {
                threadInfo = args.ThreadId;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                RuntimeThreadSuspended = new Notify_RuntimeThreadSuspended() { ThreadId = suspendedThreadId.ToUInt64() },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(suspendedThreadId, threadInfo);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_RuntimeThreadResumed()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr resumedThreadId = new(321);

            var raised = false;
            var threadInfo = default(UIntPtr);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.RuntimeThreadResumed += args =>
            {
                threadInfo = args.ThreadId;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                RuntimeThreadResumed = new Notify_RuntimeThreadResumed() { ThreadId = resumedThreadId.ToUInt64() },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(resumedThreadId, threadInfo);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_GarbageCollectionStarted()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            var range = new COR_PRF_GC_GENERATION_RANGE
            {
                generation = COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_2,
                rangeStart = new(123),
                rangeLength = new(456),
                rangeLengthReserved = new(789)
            };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[] { range };
            var boundsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref bounds[0], bounds.Length));

            var raised = false;
            var generationInfos = default(bool[]);
            var boundInfos = default(COR_PRF_GC_GENERATION_RANGE[]);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.GarbageCollectionStarted += args =>
            {
                generationInfos = args.GenerationsCollected;
                boundInfos = args.Bounds;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                GarbageCollectionStarted = new Notify_GarbageCollectionStarted() 
                {
                    GenerationSegmentBounds = ByteString.CopyFrom(boundsBytes)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(bounds, boundInfos);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_GarbageCollectionFinished()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            var range = new COR_PRF_GC_GENERATION_RANGE
            {
                generation = COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_2,
                rangeStart = new(123),
                rangeLength = new(456),
                rangeLengthReserved = new(789)
            };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[] { range };
            var boundsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref bounds[0], bounds.Length));

            var raised = false;
            var boundInfos = default(COR_PRF_GC_GENERATION_RANGE[]);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.GarbageCollectionFinished += args =>
            {
                boundInfos = args.Bounds;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                GarbageCollectionFinished = new Notify_GarbageCollectionFinished()
                {
                    GenerationSegmentBounds = ByteString.CopyFrom(boundsBytes)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(bounds, boundInfos);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_SurvivingReferences()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr[] startIds = new UIntPtr[] { new(123), new(456) };
            uint[] lengths = new uint[] { 789, 987 };
            var startIdsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref startIds[0], startIds.Length));
            var lengthsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref lengths[0], lengths.Length));

            var raised = false;
            var startInfos = default(UIntPtr[]);
            var lengthInfos = default(uint[]);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.SurvivingReferences += args =>
            {
                startInfos = args.BlockStarts;
                lengthInfos = args.Lengths;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                SurvivingReferences = new Notify_SurvivingReferences()
                {
                    Blocks = ByteString.CopyFrom(startIdsBytes),
                    Lengths = ByteString.CopyFrom(lengthsBytes)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(startIds, startInfos);
            Assert.Equal(lengths, lengthInfos);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }

        [Fact]
        public void ProfilingMessageHub_MovedReferences()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr[] oldStartIds = new UIntPtr[] { new(123), new(456) };
            UIntPtr[] newStartIds = new UIntPtr[] { new(234), new(567) };
            uint[] lengths = new uint[] { 789, 987 };
            var oldStartIdsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref oldStartIds[0], oldStartIds.Length));
            var newStartIdsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref newStartIds[0], newStartIds.Length));
            var lengthsBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref lengths[0], lengths.Length));

            var raised = false;
            var oldStartInfos = default(UIntPtr[]);
            var newStartInfos = default(UIntPtr[]);
            var lengthInfos = default(uint[]);
            var eventInfo = default(RawEventInfo);
            var hub = new ProfilingMessageHub(LoggerFactory);
            hub.MovedReferences += args =>
            {
                oldStartInfos = args.OldBlockStarts;
                newStartInfos = args.NewBlockStarts;
                lengthInfos = args.Lengths;
                eventInfo = args.Info;
                raised = true;
            };

            // Act
            hub.Process(new NotifyMessage()
            {
                MovedReferences = new Notify_MovedReferences()
                {
                    OldBlocks = ByteString.CopyFrom(oldStartIdsBytes),
                    NewBlocks = ByteString.CopyFrom(newStartIdsBytes),
                    Lengths = ByteString.CopyFrom(lengthsBytes)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(oldStartIds, oldStartInfos);
            Assert.Equal(newStartIds, newStartInfos);
            Assert.Equal(lengths, lengthInfos);
            Assert.Equal(notificationId, eventInfo.Id);
            Assert.Equal(processId, eventInfo.ProcessId);
            Assert.Equal(threadId, eventInfo.ThreadId);
        }
    }
}
