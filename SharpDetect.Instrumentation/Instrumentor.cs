using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Dnlib.Extensions;
using System.Collections.Immutable;

namespace SharpDetect.Instrumentation
{
    internal class Instrumentor : IInstrumentor
    {
        private ImmutableDictionary<int, CodeInjector> codeInjectors;
        private ImmutableDictionary<int, CodeInspector> codeInspectors;
        private readonly IProfilingClient profilingClient;
        private readonly IModuleBindContext moduleBindContext;
        private readonly IMetadataContext metadataContext;
        private readonly IStringHeapCache stringHeapCache;
        private readonly IMethodDescriptorRegistry methodDescriptorRegistry;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<Instrumentor> logger;
        private volatile int instrumentedMethodsCount;
        private volatile int injectedMethodWrappersCount;
        private volatile int injectedMethodHooksCount;

        public int InstrumentedMethodsCount { get => instrumentedMethodsCount; }
        public int InjectedMethodWrappersCount { get => injectedMethodWrappersCount; }
        public int InjectedMethodHooksCount { get => injectedMethodHooksCount; }

        public Instrumentor(
            IShadowExecutionObserver executionObserver, 
            IProfilingClient profilingClient, 
            IModuleBindContext moduleBindContext,
            IMetadataContext metadataContext,
            IStringHeapCache stringHeapCache,
            IMethodDescriptorRegistry methodDescriptorRegistry,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            this.profilingClient = profilingClient;
            this.moduleBindContext = moduleBindContext;
            this.metadataContext = metadataContext;
            this.stringHeapCache = stringHeapCache;
            this.methodDescriptorRegistry = methodDescriptorRegistry;
            this.serviceProvider = serviceProvider;
            this.logger = loggerFactory.CreateLogger<Instrumentor>();

            codeInjectors = ImmutableDictionary<int, CodeInjector>.Empty;
            codeInspectors = ImmutableDictionary<int, CodeInspector>.Empty;

            executionObserver.ModuleLoaded += ExecutionObserver_ModuleLoaded;
            executionObserver.JITCompilationStarted += ExecutionObserver_JITCompilationStarted;
        }

        /// <summary>
        /// Resolve ModuleLoaded event and issue metadata modification requests
        /// </summary>
        private void ExecutionObserver_ModuleLoaded((IShadowCLR Runtime, ModuleInfo Module, string Path, EventInfo Info) args)
        {
            // Fetch module definition
            var moduleDef = moduleBindContext.LoadModule(
                args.Info.ProcessId,
                args.Path,
                args.Module);

            // Respond to profiler if any metadata changes are necessary
            if (GetMethodsToWrap(moduleDef, args.Module.Id).Any())
            {
                // Issue wrapping request
                profilingClient.IssueEmitMethodWrappersRequestAsync(GetMethodsToWrap(moduleDef, args.Module.Id), args.Info).Wait();
            }
            else
            {
                // Issue no changes request (there was nothing to wrap)
                profilingClient.IssueNoChangesRequestAsync(args.Info).Wait();
            }
        }

        /// <summary>
        /// Resolve JITCompilation started event and issue bytecode instrumentation requests
        /// </summary>
        private void ExecutionObserver_JITCompilationStarted((IShadowCLR Runtime, FunctionInfo Function, EventInfo Info) args)
        {
            // Fetch method definition
            var resolver = metadataContext.GetResolver(args.Info.ProcessId);
            var moduleInfo = new ModuleInfo(args.Function.ModuleId);
            if (!resolver.TryGetMethodDef(args.Function, moduleInfo, out var method))
            {
                // If we were unable to resolve the method it was created dynamically:
                // 1.) either is a profiler method TODO: recognize these
                // 2.) program emitted a method through reflection or similar technique
                profilingClient.IssueNoChangesRequestAsync(args.Info).Wait();
                return;
            }

            // We can handle only methods that are managed
            if (method.Body is null)
            {
                // There is nothing we can do about this method
                profilingClient.IssueNoChangesRequestAsync(args.Info).Wait();
                return;
            }

            // Check if we need to patch any calls to wrapped extern methods
            var stubs = WrapAnalyzedExternMethodCalls(method, moduleInfo, args.Info);

            // Check if user requested instrumentation for this method
            if (methodDescriptorRegistry.TryGetMethodInterpretationData(method, out var data))
            {
                var assembler = new FastMethodAssembler(method, stubs, stringHeapCache);
                var bytecode = assembler.Assemble();

                if (data.Flags.HasFlag(MethodRewritingFlags.InjectEntryExitHooks))
                    Interlocked.Increment(ref injectedMethodHooksCount);
                if (bytecode is not null)
                    Interlocked.Increment(ref instrumentedMethodsCount);

                profilingClient.IssueRewriteMethodBodyAsync(null, data, args.Info).Wait();
            }
        }

        private Dictionary<Instruction, MDToken> WrapAnalyzedExternMethodCalls(MethodDef method, ModuleInfo moduleInfo, EventInfo info)
        {
            var stubs = new Dictionary<Instruction, MDToken>();
            var resolver = metadataContext.GetResolver(info.ProcessId);

            foreach (var instruction in method.Body.Instructions.Where(i => i.OpCode.Code == Code.Call && i.Operand is IMethodDefOrRef))
            {
                MDToken wrapperToken = default;

                if (resolver.TryResolveMethodDef((instruction.Operand as IMethodDefOrRef)!, out var methodDef))
                {
                    if (methodDef.HasBody)
                    {
                        // This is not an extern method
                        continue;
                    }

                    if (!resolver.TryGetWrapperMethodReference(methodDef, moduleInfo, out wrapperToken))
                    {
                        // No wrapper registered
                        continue;
                    }
                }
                else
                {
                    // We might need to resolve a method defined within a module that has not been loaded yet
                    // Slow-path: try to match the method based on given reference
                    if (!resolver.TryLookupWrapperMethodReference((instruction.Operand as IMethodDefOrRef)!, moduleInfo, out wrapperToken))
                    {
                        // No wrapper registered
                        continue;
                    }
                }

                stubs.Add(instruction, wrapperToken);
            }

            return stubs;
        }

        private (CodeInspector Inspector, CodeInjector Injector) Register(int processId)
        {
            var newInspector = new CodeInspector();
            var newInjector = new CodeInjector();

            codeInspectors.Add(processId, newInspector);
            codeInjectors.Add(processId, newInjector);
            return (newInspector, newInjector);
        }

        private CodeInspector GetCodeInspector(int processId)
        {
            if (!codeInspectors.TryGetValue(processId, out var inspector))
            {
                lock (codeInspectors)
                {
                    // Make sure each process gets initialized only once
                    if (!codeInspectors.TryGetValue(processId, out inspector))
                        return Register(processId).Inspector;
                }
            }

            return inspector;
        }

        private CodeInjector GetCodeInjector(int processId)
        {
            if (!codeInjectors.TryGetValue(processId, out var injector))
            {
                lock (codeInspectors)
                {
                    // Make sure each process gets initialized only once
                    if (!codeInjectors.TryGetValue(processId, out injector))
                        return Register(processId).Injector;
                }
            }

            return injector;
        }

        private IEnumerable<(FunctionInfo, ushort)> GetMethodsToWrap(ModuleDef moduleDef, UIntPtr moduleId)
        {
            foreach (var (id, data) in methodDescriptorRegistry.GetRegisteredMethods(moduleDef.Assembly.Name)
                .Where(e => e.Data.Flags.HasFlag(MethodRewritingFlags.InjectManagedWrapper)))
            {
                // Find declaring type of the extern method
                var declaringTypeDef = moduleDef.Find(id.DeclaringType, isReflectionName: true);
                // Resolve method signature
                var externMethodSignature = MethodSig.CreateStatic(
                    retType: moduleDef.CorLibTypes.Void,
                    argTypes: id.ArgumentTypes.Select(a => moduleDef.Find(a, isReflectionName: true).ToTypeSig()).ToArray());
                // Find extern method
                var externMethodDef = declaringTypeDef.FindMethod(id.Name, externMethodSignature);

                Interlocked.Increment(ref injectedMethodWrappersCount);
                yield return (new FunctionInfo(moduleId, declaringTypeDef.MDToken, externMethodDef.MDToken), id.ArgsCount);
            }
        }
    }
}
