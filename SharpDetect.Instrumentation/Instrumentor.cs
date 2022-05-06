using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using System.Collections.Immutable;

namespace SharpDetect.Instrumentation
{
    internal class Instrumentor : IInstrumentor
    {
        private ImmutableDictionary<int, CodeInjector> codeInjectors;
        private ImmutableDictionary<int, CodeInspector> codeInspectors;
        private readonly IProfilingClient profilingClient;
        private readonly IShadowExecutionObserver executionObserver;
        private readonly IModuleBindContext moduleBindContext;
        private readonly IMetadataContext metadataContext;
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
            IMethodDescriptorRegistry methodDescriptorRegistry,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            this.executionObserver = executionObserver;
            this.profilingClient = profilingClient;
            this.moduleBindContext = moduleBindContext;
            this.metadataContext = metadataContext;
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
            if (!resolver.TryGetMethodDef(args.Function, new ModuleInfo(args.Function.ModuleId), out var method))
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
            // TODO:

            // Check if user requested instrumentation for this method
            if (methodDescriptorRegistry.TryGetMethodInterpretationData(method, out var data))
            {
                // TODO: get new IL body
                var bytecode = null as byte[];

                if (data.Flags.HasFlag(MethodRewritingFlags.InjectEntryExitHooks))
                    Interlocked.Increment(ref injectedMethodHooksCount);
                if (bytecode is not null)
                    Interlocked.Increment(ref instrumentedMethodsCount);

                profilingClient.IssueRewriteMethodBodyAsync(null, data, args.Info).Wait();
            }
        }

        private (CodeInspector Inspector, CodeInjector Injector) Register(int processId)
        {

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
