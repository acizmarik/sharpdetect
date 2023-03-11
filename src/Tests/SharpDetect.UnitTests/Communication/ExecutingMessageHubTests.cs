// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Google.Protobuf;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services;
using SharpDetect.Core.Communication;
using Xunit;

namespace SharpDetect.UnitTests.Communication
{
    public class ExecutingMessageHubTests : TestsBase
    {
        [Fact]
        public async Task ExecutingMessageHub_MethodCalled()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(ExecutingMessageHubTests).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = CreateModuleBindContext();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(ExecutingMessageHub_MethodCalled));

            var raised = false;
            var function = default(FunctionInfo);
            var arguments = default(RawArgumentsList?);
            var info = default(RawEventInfo);
            var executingMessageHub = new ExecutingMessageHub(LoggerFactory);
            executingMessageHub.MethodCalled += args =>
            {
                function = args.Function;
                arguments = args.Arguments;
                info = args.Info;
                raised = true;
            };

            // Act
            profilingMessageHub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.Null(arguments);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_MethodCalled_Static_WithArguments()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(ExecutingMessageHubTests).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = CreateModuleBindContext();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(StaticTestMethod));
            byte[] argValues = new byte[] { 123, 0, 0, 0 /* Value (int) */ };
            byte[] argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };

            var raised = false;
            var function = default(FunctionInfo);
            var arguments = default(RawArgumentsList?);
            var info = default(RawEventInfo);
            var executingMessageHub = new ExecutingMessageHub(LoggerFactory);
            executingMessageHub.MethodCalled += args =>
            {
                function = args.Function;
                arguments = args.Arguments;
                info = args.Info;
                raised = true;
            };

            // Act
            profilingMessageHub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets),
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.NotNull(arguments);
            Assert.Equal(argValues, arguments!.Value.ArgValues);
            Assert.Equal(argOffsets, arguments!.Value.ArgOffsets);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_MethodCalled_Instance_WithArguments()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(ExecutingMessageHubTests).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = CreateModuleBindContext();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(InstanceTestMethod));
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[]
                {
                    11, 0, 0, 0 /* Instance (UIntPtr) */,
                    123, 0, 0, 0 /* Value (int) */ };
                argOffsets = new byte[]
                {
                    4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */,
                    4, 0 /* Value length (ushort) */, 1, 0 /* Index (ushort) */ 
                };
            }
            else
            {
                argValues = new byte[]
                {
                    11, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */,
                    123, 0, 0, 0 /* Value (int) */ 
                };
                argOffsets = new byte[]
                {
                    8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */,
                    4, 0 /* Value length (ushort) */, 1, 0 /* Index (ushort) */ 
                };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var arguments = default(RawArgumentsList?);
            var info = default(RawEventInfo);
            var executingMessageHub = new ExecutingMessageHub(LoggerFactory);
            executingMessageHub.MethodCalled += args =>
            {
                function = args.Function;
                arguments = args.Arguments;
                info = args.Info;
                raised = true;
            };

            // Act
            profilingMessageHub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets),
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.NotNull(arguments);
            Assert.Equal(argValues, arguments!.Value.ArgValues.ToByteArray());
            Assert.Equal(argOffsets, arguments!.Value.ArgOffsets.ToByteArray());
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_MethodReturned()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(ExecutingMessageHubTests).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = CreateModuleBindContext();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(ExecutingMessageHub_MethodCalled));

            var raised = false;
            var function = default(FunctionInfo);
            var retValue = default(RawReturnValue?);
            var byRefArguments = default(RawArgumentsList?);
            var info = default(RawEventInfo);
            var executingMessageHub = new ExecutingMessageHub(LoggerFactory);
            executingMessageHub.MethodReturned += args =>
            {
                function = args.Function;
                retValue = args.ReturnValue;
                byRefArguments = args.ByRefArguments;
                info = args.Info;
                raised = true;
            };

            // Act
            profilingMessageHub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.Null(retValue);
            Assert.Null(byRefArguments);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Theory]
        [InlineData(nameof(StaticTestMethod))]
        [InlineData(nameof(InstanceTestMethod))]
        public async Task ExecutingMessageHub_MethodReturned_WithRetValue(string methodName)
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(ExecutingMessageHubTests).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = CreateModuleBindContext();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == methodName);
            byte[] returnValueArray = new byte[] { 123, 0, 0, 0 /* Value (int) */ };

            var raised = false;
            var function = default(FunctionInfo);
            var retValue = default(RawReturnValue?);
            var byRefArguments = default(RawArgumentsList?);
            var info = default(RawEventInfo);
            var executingMessageHub = new ExecutingMessageHub(LoggerFactory);
            executingMessageHub.MethodReturned += args =>
            {
                function = args.Function;
                retValue = args.ReturnValue;
                byRefArguments = args.ByRefArguments;
                info = args.Info;
                raised = true;
            };

            // Act
            profilingMessageHub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                    ReturnValue = ByteString.CopyFrom(returnValueArray)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.NotNull(retValue);
            Assert.Equal(returnValueArray, retValue!.Value.ReturnValue.ToByteArray());
            Assert.Null(byRefArguments);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Theory]
        [InlineData(nameof(StaticTestMethodRefArg), true)]
        [InlineData(nameof(StaticTestMethodOutArg), true)]
        [InlineData(nameof(InstanceTestMethodRefArg), false)]
        [InlineData(nameof(InstanceTestMethodOutArg), false)]
        public async Task ExecutingMessageHub_MethodReturned_WithByRefArguments(string methodName, bool isStaticMethod)
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(ExecutingMessageHubTests).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = CreateModuleBindContext();
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == methodName);
            byte[] byRefArgValuesArray = new byte[] { 123, 0, 0, 0 /* Value (int) */ };
            byte[] byRefArgOffsetsArray = new byte[] { 4, 0 /* Value length (ushort) */, (isStaticMethod) ? (byte)0 : (byte)1, 0 /* Index (ushort) */ };

            var raised = false;
            var function = default(FunctionInfo);
            var retValue = default(RawReturnValue?);
            var byRefArguments = default(RawArgumentsList?);
            var info = default(RawEventInfo);
            var executingMessageHub = new ExecutingMessageHub(LoggerFactory);
            executingMessageHub.MethodReturned += args =>
            {
                function = args.Function;
                retValue = args.ReturnValue;
                byRefArguments = args.ByRefArguments;
                info = args.Info;
                raised = true;
            };

            // Act
            profilingMessageHub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw,
                    ByRefArgumentValues = ByteString.CopyFrom(byRefArgValuesArray),
                    ByRefArgumentOffsets = ByteString.CopyFrom(byRefArgOffsetsArray)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.Null(retValue);
            Assert.NotNull(byRefArguments);
            Assert.Equal(byRefArgValuesArray, byRefArguments!.Value.ArgValues.ToByteArray());
            Assert.Equal(byRefArgOffsetsArray, byRefArguments!.Value.ArgOffsets.ToByteArray());
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        private static int StaticTestMethod(int arg) => arg;
        private int InstanceTestMethod(int arg) => arg;

        private static int StaticTestMethodRefArg(ref int arg) => arg * 2;
        private static int StaticTestMethodOutArg(out int arg) => arg = 123;
        private int InstanceTestMethodRefArg(ref int arg) => arg * 2;
        private int InstanceTestMethodOutArg(out int arg) => arg = 123;
    }
}
