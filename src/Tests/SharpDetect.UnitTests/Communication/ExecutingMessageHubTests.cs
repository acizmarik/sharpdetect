using dnlib.DotNet;
using Google.Protobuf;
using SharpDetect.Common;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Core.Communication;
using Xunit;

namespace SharpDetect.UnitTests.Communication
{
    public class ExecutingMessageHubTests : TestsBase
    {
        [Theory]
        [InlineData(123, true)]
        [InlineData(132, false)]
        public async Task ExecutingMessageHub_FieldAccessed(byte expectedIdentifier, bool expectedIsWrite)
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            ModuleInfo moduleInfo = new ModuleInfo(moduleId);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var coreLibModule = moduleBindContext.LoadModule(processId, modulePath, moduleInfo);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            byte[] argValues = new byte[]
            {
                (expectedIsWrite) ? (byte)1 : (byte)0 /* Value (bool) */,
                expectedIdentifier, 0, 0, 0, 0, 0, 0, 0 /* Value (ulong) */
            };
            byte[] argOffsets = new byte[]
            { 
                1, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */,
                8, 0 /* Value length (ushort) */, 1, 0 /* Index (ushort) */
            };

            var raised = false;
            var isWrite = default(bool);
            var identifier = default(ulong);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.FieldAccessed += args =>
            {
                isWrite = args.IsWrite;
                identifier = args.Identifier;
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
            profilingMessageHub.Process(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = moduleId.ToUInt64(),
                    ModulePath = modulePath
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            var helpersMapping = MockInjectCoreLib(processId, moduleInfo, coreLibModule, metadataContext);
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = helpersMapping[MethodType.FieldAccess].TypeToken.Raw,
                    FunctionToken = helpersMapping[MethodType.FieldAccess].FunctionToken.Raw,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(expectedIdentifier, identifier);
            Assert.Equal(expectedIsWrite, isWrite);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_FieldInstanceAccessed()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            ModuleInfo moduleInfo = new ModuleInfo(moduleId);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var coreLibModule = moduleBindContext.LoadModule(processId, modulePath, moduleInfo);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            byte[] argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Value (UIntPtr) */ };
            byte[] argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };

            var raised = false;
            var instance = default(UIntPtr);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.FieldInstanceAccessed += args =>
            {
                instance = args.Instance;
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
            profilingMessageHub.Process(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = moduleId.ToUInt64(),
                    ModulePath = modulePath
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            var helpersMapping = MockInjectCoreLib(processId, moduleInfo, coreLibModule, metadataContext);
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = helpersMapping[MethodType.FieldInstanceAccess].TypeToken.Raw,
                    FunctionToken = helpersMapping[MethodType.FieldInstanceAccess].FunctionToken.Raw,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(new(123), instance);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Theory]
        [InlineData(123, true)]
        [InlineData(132, false)]
        public async Task ExecutingMessageHub_ArrayElementAccessed(byte expectedIdentifier, bool expectedIsWrite)
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            ModuleInfo moduleInfo = new ModuleInfo(moduleId);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var coreLibModule = moduleBindContext.LoadModule(processId, modulePath, moduleInfo);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            byte[] argValues = new byte[]
            {
                (expectedIsWrite) ? (byte)1 : (byte)0 /* Value (bool) */,
                expectedIdentifier, 0, 0, 0, 0, 0, 0, 0 /* Value (ulong) */
            };
            byte[] argOffsets = new byte[]
            {
                1, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */,
                8, 0 /* Value length (ushort) */, 1, 0 /* Index (ushort) */
            };

            var raised = false;
            var isWrite = default(bool);
            var identifier = default(ulong);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ArrayElementAccessed += args =>
            {
                isWrite = args.IsWrite;
                identifier = args.Identifier;
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
            profilingMessageHub.Process(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = moduleId.ToUInt64(),
                    ModulePath = modulePath
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            var helpersMapping = MockInjectCoreLib(processId, moduleInfo, coreLibModule, metadataContext);
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = helpersMapping[MethodType.ArrayElementAccess].TypeToken.Raw,
                    FunctionToken = helpersMapping[MethodType.ArrayElementAccess].FunctionToken.Raw,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(expectedIdentifier, identifier);
            Assert.Equal(expectedIsWrite, isWrite);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_ArrayInstanceAccessed()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            ModuleInfo moduleInfo = new ModuleInfo(moduleId);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var coreLibModule = moduleBindContext.LoadModule(processId, modulePath, moduleInfo);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            byte[] argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Value (UIntPtr) */ };
            byte[] argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };

            var raised = false;
            var instance = default(UIntPtr);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ArrayInstanceAccessed += args =>
            {
                instance = args.Instance;
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
            profilingMessageHub.Process(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = moduleId.ToUInt64(),
                    ModulePath = modulePath
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            var helpersMapping = MockInjectCoreLib(processId, moduleInfo, coreLibModule, metadataContext);
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = helpersMapping[MethodType.ArrayInstanceAccess].TypeToken.Raw,
                    FunctionToken = helpersMapping[MethodType.ArrayInstanceAccess].FunctionToken.Raw,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(new(123), instance);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_ArrayIndexAccessed()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            ModuleInfo moduleInfo = new ModuleInfo(moduleId);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var coreLibModule = moduleBindContext.LoadModule(processId, modulePath, moduleInfo);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            byte[] argValues = new byte[] { 123, 0, 0, 0, /* Value (int) */ };
            byte[] argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };

            var raised = false;
            var index = default(int);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ArrayIndexAccessed += args =>
            {
                index = args.Index;
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
            profilingMessageHub.Process(new NotifyMessage()
            {
                ModuleLoaded = new Notify_ModuleLoaded()
                {
                    ModuleId = moduleId.ToUInt64(),
                    ModulePath = modulePath
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });
            var helpersMapping = MockInjectCoreLib(processId, moduleInfo, coreLibModule, metadataContext);
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodCalled = new Notify_MethodCalled()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = helpersMapping[MethodType.ArrayIndexAccess].TypeToken.Raw,
                    FunctionToken = helpersMapping[MethodType.ArrayIndexAccess].FunctionToken.Raw,
                    ArgumentValues = ByteString.CopyFrom(argValues),
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.Equal(123, index);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

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
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(ExecutingMessageHub_MethodCalled));

            var raised = false;
            var function = default(FunctionInfo);
            var arguments = default((ushort, IValueOrPointer)[]);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
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
            Assert.Empty(arguments);
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
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(StaticTestMethod));
            byte[] argValues = new byte[] { 123, 0, 0, 0 /* Value (int) */ };
            byte[] argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };

            var raised = false;
            var function = default(FunctionInfo);
            var arguments = default((ushort, IValueOrPointer)[]);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
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
            Assert.NotEmpty(arguments);
            Assert.Single(arguments);
            Assert.Equal(0, arguments![0].Item1);
            Assert.True(arguments![0].Item2.HasValue());
            Assert.Equal(123, (int)arguments![0].Item2.BoxedValue!);
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
            var moduleBindContext = ModuleBindContext;
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
            var arguments = default((ushort, IValueOrPointer)[]);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
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
            Assert.NotEmpty(arguments);
            Assert.Equal(2, arguments!.Length);
            Assert.Equal(0, arguments![0].Item1);
            Assert.True(arguments![0].Item2.HasPointer());
            Assert.Equal(11u, arguments![0].Item2.Pointer!.Value.ToUInt64());
            Assert.True(arguments![1].Item2.HasValue());
            Assert.Equal(123, (int)arguments![1].Item2.BoxedValue!);
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
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(ExecutingMessageHub_MethodCalled));

            var raised = false;
            var function = default(FunctionInfo);
            var retValue = default(IValueOrPointer);
            var byRefArguments = default((ushort, IValueOrPointer)[]);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
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
            Assert.Empty(byRefArguments);
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
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == methodName);
            byte[] returnValueArray = new byte[] { 123, 0, 0, 0 /* Value (int) */ };

            var raised = false;
            var function = default(FunctionInfo);
            var retValue = default(IValueOrPointer);
            var byRefArguments = default((ushort, IValueOrPointer)[]);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
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
            Assert.True(retValue!.HasValue());
            Assert.Equal(123, (int)retValue.BoxedValue!);
            Assert.Empty(byRefArguments);
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
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync();
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(ExecutingMessageHubTests));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == methodName);
            byte[] byRefArgValuesArray = new byte[] { 123, 0, 0, 0 /* Value (int) */ };
            byte[] byRefArgOffsetsArray = new byte[] { 4, 0 /* Value length (ushort) */, (isStaticMethod) ? (byte)0 : (byte)1, 0 /* Index (ushort) */ };

            var raised = false;
            var function = default(FunctionInfo);
            var retValue = default(IValueOrPointer);
            var byRefArguments = default((ushort, IValueOrPointer)[]);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
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
            Assert.NotEmpty(byRefArguments);
            Assert.Single(byRefArguments);
            Assert.True(byRefArguments![0].Item2.HasValue());
            Assert.Equal(123, (int)byRefArguments[0].Item2.BoxedValue!);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_LockAcquireAttempted()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(Monitor).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Enter) && m.Parameters.Count == 1);
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var instance = default(UIntPtr);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.LockAcquireAttempted += args =>
            {
                function = args.Function;
                instance = args.Instance;
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
            Assert.Equal(123u, instance.ToUInt64());
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_LockAcquireReturned()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Enter) && m.Parameters.Count == 1);
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var isSuccess = default(bool);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.LockAcquireReturned += args =>
            {
                function = args.Function;
                isSuccess = args.IsSuccess;
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
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
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
            Assert.True(isSuccess);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_LockReleaseCalled()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(Monitor).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Exit));
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var instance = default(UIntPtr);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.LockReleaseCalled += args =>
            {
                function = args.Function;
                instance = args.Instance;
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
            Assert.Equal(123u, instance.ToUInt64());
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_LockReleaseReturned()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Exit) && m.Parameters.Count == 1);
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.LockReleaseReturned += args =>
            {
                function = args.Function;
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
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
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
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_ObjectWaitAttempted()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(Monitor).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Wait) 
                && m.Parameters.Count == 2 && m.Parameters[1].Type.FullName == typeof(int).FullName);
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var instance = default(UIntPtr);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ObjectWaitAttempted += args =>
            {
                function = args.Function;
                instance = args.Instance;
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
            Assert.Equal(123u, instance.ToUInt64());
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_ObjectWaitReturned()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(object).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Wait) && m.Parameters.Count == 1);
            byte[] returnValueArray = new byte[] { 1, 0, 0, 0 /* Value (bool) */ };
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var isSuccess = default(bool);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ObjectWaitReturned += args =>
            {
                function = args.Function;
                isSuccess = args.IsSuccess;
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
                    ArgumentOffsets = ByteString.CopyFrom(argOffsets)
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
            Assert.True(isSuccess);
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_ObjectPulseCalled()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(Monitor).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Pulse));
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var instance = default(UIntPtr);
            var isPulseAll = default(bool);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ObjectPulseCalled += args =>
            {
                function = args.Function;
                instance = args.Instance;
                isPulseAll = args.IsPulseAll;
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
            Assert.False(isPulseAll);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.Equal(123u, instance.ToUInt64());
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_ObjectPulseAllCalled()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(Monitor).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.PulseAll));
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var instance = default(UIntPtr);
            var isPulseAll = default(bool);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ObjectPulseCalled += args =>
            {
                function = args.Function;
                instance = args.Instance;
                isPulseAll = args.IsPulseAll;
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
            Assert.True(isPulseAll);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
            Assert.Equal(123u, instance.ToUInt64());
            Assert.Equal(notificationId, info.Id);
            Assert.Equal(processId, info.ProcessId);
        }

        [Fact]
        public async Task ExecutingMessageHub_ObjectPulseReturned()
        {
            // Prepare
            const ulong notificationId = 123;
            const int processId = 456;
            UIntPtr threadId = new(789);
            UIntPtr moduleId = new(987);
            string modulePath = typeof(Monitor).Assembly.Location;
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var moduleBindContext = ModuleBindContext;
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var methodDataRegistry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            ModuleDef moduleDef = moduleBindContext.LoadModule(processId, modulePath, new(moduleId));
            TypeDef typeDef = moduleDef.Types.Single(t => t.Name == nameof(Monitor));
            MethodDef methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.PulseAll));
            byte[] argValues;
            byte[] argOffsets;
            if (UIntPtr.Size == 4)
            {
                argValues = new byte[] { 123, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 4, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }
            else
            {
                argValues = new byte[] { 123, 0, 0, 0, 0, 0, 0, 0 /* Instance (UIntPtr) */ };
                argOffsets = new byte[] { 8, 0 /* Value length (ushort) */, 0, 0 /* Index (ushort) */ };
            }

            var raised = false;
            var function = default(FunctionInfo);
            var isPulseAll = default(bool);
            var info = default(EventInfo);
            var executingMessageHub = new ExecutingMessageHub(metadataContext, methodDataRegistry, LoggerFactory);
            executingMessageHub.ObjectPulseReturned += args =>
            {
                function = args.Function;
                isPulseAll = args.IsPulseAll;
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
            executingMessageHub.Process(new NotifyMessage()
            {
                MethodReturned = new Notify_MethodReturned()
                {
                    ModuleId = moduleId.ToUInt64(),
                    TypeToken = typeDef.MDToken.Raw,
                    FunctionToken = methodDef.MDToken.Raw
                },
                ProcessId = processId,
                ThreadId = threadId.ToUInt64(),
                NotificationId = notificationId
            });

            // Assert
            Assert.True(raised);
            Assert.True(isPulseAll);
            Assert.Equal(moduleId, function.ModuleId);
            Assert.Equal(typeDef.MDToken, function.TypeToken);
            Assert.Equal(methodDef.MDToken, function.FunctionToken);
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
