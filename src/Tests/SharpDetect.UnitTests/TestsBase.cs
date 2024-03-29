﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.MD;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NativeObjects;
using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Models;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Scripts;
using SharpDetect.Loader;
using SharpDetect.Metadata;
using SharpDetect.Profiler;

namespace SharpDetect.UnitTests
{
    public abstract class TestsBase
    {
        public readonly LoggerFactory LoggerFactory;
        public readonly ServiceCollection ServiceCollection;
        private readonly AssemblyLoadContext sharedAssemblyLoadContext;

        public static IMetadataContext CreateMetadataContext(IModuleBindContext moduleBindContext, IProfilingMessageHub profilingMessageHub)
        {
            return new MetadataContext(
                moduleBindContext,
                profilingMessageHub);
        }

        public IModuleBindContext CreateModuleBindContext(bool cleanAssemblyContext = false)
        {
            var assemblyContext = (cleanAssemblyContext) ? new AssemblyLoadContext(LoggerFactory) : sharedAssemblyLoadContext;
            return new ModuleBindContext(assemblyContext, LoggerFactory);
        }

        public IServiceProvider BuildServiceProvider(params (Type, object)[] singletons)
        {
            var collection = new ServiceCollection();
            foreach (var (type, service) in singletons)
                collection.AddSingleton(type, service);
            return collection.BuildServiceProvider();
        }

        public TestsBase()
        {
            LoggerFactory = new LoggerFactory();
            ServiceCollection = new ServiceCollection();
            sharedAssemblyLoadContext = new AssemblyLoadContext(LoggerFactory);
        }

        protected IConfiguration CreateModulesConfiguration()
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> {
                    { $"{Constants.ModuleDescriptors.Core}:0", "Modules" },
                })
                .Build();
        }

        protected SharpDetect.Profiler.ICorProfilerInfo CreateFakeICorProfilerInfo()
        {
            ThreadId anOut;
            var fake = A.Fake<SharpDetect.Profiler.ICorProfilerInfo>();
            A.CallTo(() => fake.GetCurrentThreadId(out anOut))
                .Returns(HResult.S_OK)
                .AssignsOutAndRefParameters(new ThreadId((nuint)Environment.CurrentManagedThreadId));

            return fake;
        }

        protected IConfiguration CreateInstrumentorConfiguration(bool enableInstrumentation, InstrumentationStrategy strategy, string[] patterns)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>(
                    new[]
                    {
                        new KeyValuePair<string, string?>(Constants.Rewriting.Enabled, enableInstrumentation.ToString().ToLowerInvariant()),
                        new KeyValuePair<string, string?>(Constants.Rewriting.Strategy, strategy.ToString())
                    }
                    .Concat(patterns.Select((p, i) => new KeyValuePair<string, string?>($"{Constants.Rewriting.Patterns}:{i}:Pattern", p)))
                    .Concat(patterns.Select((p, i) => new KeyValuePair<string, string?>($"{Constants.Rewriting.Patterns}:{i}:Target", nameof(InstrumentationTarget.Method))))))
                .Build();
        }

        protected async Task<MethodDescriptorRegistry> CreateRegistryForModulesAsync(params string[] modules)
        {
            var registry = new MethodDescriptorRegistry();
            var luaBridge = new LuaBridge(CreateModulesConfiguration());
            foreach (var module in modules)
            {
                var descriptor = luaBridge.CreateAssemblyDescriptor(await luaBridge.LoadModuleAsync(module));
                var methodDescriptors = new List<(MethodIdentifier, MethodInterpretationData)>();
                descriptor.GetMethodDescriptors(methodDescriptors);
                registry.Register(new LibraryDescriptor(descriptor.GetAssemblyName(), descriptor.IsCoreLibrary(), methodDescriptors));
            }

            return registry;
        }

        protected static IDictionary<MethodType, FunctionInfo> MockInjectCoreLib(int processId, ModuleInfo coreLibInfo, ModuleDef coreLibDef, IMetadataContext context)
        {
            var methodImplFlags = MethodImplAttributes.Managed | MethodImplAttributes.IL;
            var methodFlags = MethodAttributes.Static | MethodAttributes.Public;
            var result = new Dictionary<MethodType, FunctionInfo>();
            var emitter = context.GetEmitter(processId);
            var dispatcher = new TypeDefUser("SharpDetect", "EventDispatcher", coreLibDef.CorLibTypes.Object.ToTypeDefOrRef());
            emitter.Emit(coreLibInfo, dispatcher, new MDToken(Table.TypeDef, 65536));
            coreLibDef.Types.Add(dispatcher);

            // Field access
            var fieldAccessHelper = new MethodDefUser("FieldAccess", MethodSig.CreateStatic(
                retType: coreLibDef.CorLibTypes.Void,
                argType1: coreLibDef.CorLibTypes.Boolean,
                argType2: coreLibDef.CorLibTypes.UInt64),
                methodImplFlags, 
                methodFlags);
            var fieldAccessHelperInfo = new FunctionInfo(coreLibInfo.Id, dispatcher.MDToken, new MDToken(Table.Method, 65536));
            result.Add(MethodType.FieldAccess, fieldAccessHelperInfo);
            emitter.Emit(coreLibInfo, fieldAccessHelper, fieldAccessHelperInfo.FunctionToken);
            emitter.Bind(MethodType.FieldAccess, fieldAccessHelperInfo);
            dispatcher.Methods.Add(fieldAccessHelper);

            // Field instance access
            var fieldInstanceAccessHelper = new MethodDefUser("FieldInstanceAccess", MethodSig.CreateStatic(
                retType: coreLibDef.CorLibTypes.Void,
                argType1: coreLibDef.CorLibTypes.Object),
                methodImplFlags,
                methodFlags);
            var fieldInstanceAccessHelperInfo = new FunctionInfo(coreLibInfo.Id, dispatcher.MDToken, new MDToken(Table.Method, 65537));
            result.Add(MethodType.FieldInstanceAccess, fieldInstanceAccessHelperInfo);
            emitter.Emit(coreLibInfo, fieldInstanceAccessHelper, fieldInstanceAccessHelperInfo.FunctionToken);
            emitter.Bind(MethodType.FieldInstanceAccess, fieldInstanceAccessHelperInfo);
            dispatcher.Methods.Add(fieldInstanceAccessHelper);

            // Array element access
            var arrayElementAccessHelper = new MethodDefUser("ArrayElementAccess", MethodSig.CreateStatic(
                retType: coreLibDef.CorLibTypes.Void,
                argType1: coreLibDef.CorLibTypes.Boolean,
                argType2: coreLibDef.CorLibTypes.UInt64),
                methodImplFlags,
                methodFlags);
            var arrayElementAccessHelperInfo = new FunctionInfo(coreLibInfo.Id, dispatcher.MDToken, new MDToken(Table.Method, 65538));
            result.Add(MethodType.ArrayElementAccess, arrayElementAccessHelperInfo);
            emitter.Emit(coreLibInfo, arrayElementAccessHelper, arrayElementAccessHelperInfo.FunctionToken);
            emitter.Bind(MethodType.ArrayElementAccess, arrayElementAccessHelperInfo);
            dispatcher.Methods.Add(arrayElementAccessHelper);

            // Array instance element access
            var arrayInstanceAccessHelper = new MethodDefUser("ArrayInstanceAccess", MethodSig.CreateStatic(
                retType: coreLibDef.CorLibTypes.Void,
                argType1: coreLibDef.CorLibTypes.Object),
                methodImplFlags,
                methodFlags);
            var arrayInstanceAccessHelperInfo = new FunctionInfo(coreLibInfo.Id, dispatcher.MDToken, new MDToken(Table.Method, 65539));
            result.Add(MethodType.ArrayInstanceAccess, arrayInstanceAccessHelperInfo);
            emitter.Emit(coreLibInfo, arrayInstanceAccessHelper, arrayInstanceAccessHelperInfo.FunctionToken);
            emitter.Bind(MethodType.ArrayInstanceAccess, arrayInstanceAccessHelperInfo);
            dispatcher.Methods.Add(arrayInstanceAccessHelper);

            // Array index access
            var arrayIndexAccessHelper = new MethodDefUser("ArrayIndexAccess", MethodSig.CreateStatic(
                retType: coreLibDef.CorLibTypes.Void,
                argType1: coreLibDef.CorLibTypes.Int32),
                methodImplFlags,
                methodFlags);
            var arrayIndexAccessHelperInfo = new FunctionInfo(coreLibInfo.Id, dispatcher.MDToken, new MDToken(Table.Method, 65540));
            result.Add(MethodType.ArrayIndexAccess, arrayIndexAccessHelperInfo);
            emitter.Emit(coreLibInfo, arrayIndexAccessHelper, arrayIndexAccessHelperInfo.FunctionToken);
            emitter.Bind(MethodType.ArrayIndexAccess, arrayIndexAccessHelperInfo);
            dispatcher.Methods.Add(arrayIndexAccessHelper);

            return result;
        }

        protected private ShadowCLR InitiateDotnetProcessProfiling(int pid, IProfilingMessageHub profilingHub, IModuleBindContext bindContext, IMetadataContext metadataContext)
        {
            profilingHub.Process(new NotifyMessage()
            {
                ProfilerInitialized = new Notify_ProfilerInitialized(),
                ProcessId = pid,
                ThreadId = 1,
                NotificationId = 2
            });

            return new ShadowCLR(pid, metadataContext.GetResolver(pid), metadataContext.GetEmitter(pid), bindContext, new NullLoggerFactory());
        }
    }
}
