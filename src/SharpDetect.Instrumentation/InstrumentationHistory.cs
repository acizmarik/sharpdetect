// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using System.Collections.Concurrent;

namespace SharpDetect.Instrumentation
{
    internal class InstrumentationHistory : IInstrumentationHistory
    {
        public bool Enabled { get; }
        public readonly string? OutputDirectory;
        private readonly IMetadataContext metadataContext;
        private readonly IModuleBindContext moduleBindContext;
        private readonly Microsoft.Extensions.Logging.ILogger logger;
        private readonly ConcurrentDictionary<AssemblyDef, bool> hasInstrumentedMembers;
        private readonly ConcurrentDictionary<AssemblyDef, ConcurrentQueue<TypeDef>> injectedTypes;
        private readonly ConcurrentDictionary<AssemblyDef, ConcurrentQueue<MethodDef>> injectedMethods;
        private readonly ConcurrentDictionary<AssemblyDef, ConcurrentQueue<MethodDef>> injectedWrappers;

        public InstrumentationHistory(
            IConfiguration configuration,
            IMetadataContext metadataContext,
            IModuleBindContext moduleBindContext,
            IShadowExecutionObserver executionObserver,
            ILoggerFactory loggerFactory)
        {
            this.metadataContext = metadataContext;
            this.moduleBindContext = moduleBindContext;
            this.logger = loggerFactory.CreateLogger<InstrumentationHistory>();
            this.hasInstrumentedMembers = new();
            this.injectedTypes = new();
            this.injectedMethods = new();
            this.injectedWrappers = new();

            Enabled = configuration.GetSection(Constants.Verification.Enabled).Get<bool>();
            if (Enabled)
            {
                var directory = configuration.GetSection(Constants.Verification.AssembliesOutputFolder).Value;
                if (directory == null || string.IsNullOrWhiteSpace(directory))
                {
                    throw new ArgumentException("Requested IL rewriting verification, but provided no output directory " +
                        $"- parameter \"{nameof(Constants.Verification.AssembliesOutputFolder)}\" or it was empty.");
                }

                OutputDirectory = directory;
                RegisterListeners(executionObserver);
            }
        }

        public void MarkAsDirty(AssemblyDef assembly)
        {
            hasInstrumentedMembers.AddOrUpdate(assembly, true, (_, _) => true);
        }

        private void RegisterListeners(IShadowExecutionObserver executionObserver)
        {
            executionObserver.TypeInjected += args =>
            {
                var module = moduleBindContext.GetModule(args.Info.ProcessId, new(args.Type.ModuleId));
                var types = injectedTypes.GetOrAdd(module.Assembly, (_) => new ConcurrentQueue<TypeDef>());

                var moduleInfo = new ModuleInfo(args.Type.ModuleId);
                metadataContext.GetResolver(args.Info.ProcessId).TryGetTypeDef(args.Type, moduleInfo, out var typeDef);
                types.Enqueue(typeDef!);
            };

            executionObserver.MethodInjected += args =>
            {
                var module = moduleBindContext.GetModule(args.Info.ProcessId, new(args.Function.ModuleId));
                var methods = injectedMethods.GetOrAdd(module.Assembly, (_) => new ConcurrentQueue<MethodDef>());

                var moduleInfo = new ModuleInfo(args.Function.ModuleId);
                var typeInfo = new TypeInfo(args.Function.ModuleId, args.Function.TypeToken);
                metadataContext.GetResolver(args.Info.ProcessId).TryGetTypeDef(typeInfo, moduleInfo, out var typeDef);
                methods.Enqueue(MetadataGenerator.CreateHelperMethod(typeDef!, args.Type, module.CorLibTypes));
            };

            executionObserver.MethodWrapperInjected += args =>
            {
                var module = moduleBindContext.GetModule(args.Info.ProcessId, new(args.Function.ModuleId));
                var methods = injectedMethods.GetOrAdd(module.Assembly, (_) => new ConcurrentQueue<MethodDef>());

                var moduleInfo = new ModuleInfo(args.Function.ModuleId);
                metadataContext.GetResolver(args.Info.ProcessId).TryGetMethodDef(args.Function, moduleInfo, false, out var methodDef);
                methods.Enqueue(MetadataGenerator.CreateWrapper(methodDef!));
            };
        }

        public void ApplyChanges()
        {
            // Create type metadata
            foreach (var (assembly, types) in injectedTypes)
                foreach (var type in types)
                    assembly.ManifestModule.Types.Add(type);

            // Implement wrappers
            foreach (var (assembly, methods) in injectedWrappers)
            {
                foreach (var method in methods)
                {
                    var body = new CilBody();
                    foreach (var param in method.Parameters)
                        body.Instructions.Add(Instruction.Create(OpCodes.Ldarg, param));
                    body.Instructions.Add(Instruction.Create(OpCodes.Call, method));
                    body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    method.Body = body;
                }
            }
        }

        public void SaveAssemblies()
        {
            if (!Enabled)
                throw new InvalidOperationException("Changes are not tracked! Enable IL verification first.");

            if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory!);

            foreach (var assembly in 
                injectedTypes.Keys.Concat(injectedMethods.Keys).Concat(hasInstrumentedMembers.Keys).Distinct())
            {
                var path = Path.Combine(OutputDirectory!, assembly.Name + ".dll");
                logger.LogInformation("[{class}] Writing {path}", nameof(InstrumentationHistory), path);
                assembly.Write(path);
            }
        }

        public IEnumerable<MethodDef> GetAllInjectedMethodsForAssembly(AssemblyDef assembly)
        {
            var hasInjectedMethods = injectedMethods.TryGetValue(assembly, out var methods);
            var hasInjectedWrappers = injectedWrappers.TryGetValue(assembly, out var wrappers);

            return (hasInjectedMethods ? methods! : Enumerable.Empty<MethodDef>()).Concat
                (hasInjectedWrappers ? wrappers! : Enumerable.Empty<MethodDef>());
        }

        public IEnumerable<TypeDef> GetAllInjectedTypesForAssembly(AssemblyDef assembly)
        {
            if (injectedTypes.TryGetValue(assembly, out var types))
                return types;
            return Enumerable.Empty<TypeDef>();
        }
    }
}
