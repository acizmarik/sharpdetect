using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Dnlib.Extensions;
using SharpDetect.Instrumentation.Injectors;
using SharpDetect.Instrumentation.Stubs;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace SharpDetect.Instrumentation
{
    internal class Instrumentor : IInstrumentor
    {
        private readonly ConcurrentDictionary<MethodDef, (bool IsDirty, UnresolvedMethodStubs? Stubs)> instrumentationCache;
        private readonly InjectorBase[] registeredInjectors;
        private readonly ResolvedMethodStubs emptyResolvedMethodStubs;
        private ImmutableDictionary<int, CodeInjector> codeInjectors;
        private readonly IProfilingClient profilingClient;
        private readonly IModuleBindContext moduleBindContext;
        private readonly IMetadataContext metadataContext;
        private readonly IStringHeapCache stringHeapCache;
        private readonly IEventDescriptorRegistry eventDescriptorRegistry;
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
            IEventDescriptorRegistry eventDescriptorRegistry,
            IMethodDescriptorRegistry methodDescriptorRegistry,
            IEnumerable<InjectorBase> registeredInjectors,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory)
        {
            this.profilingClient = profilingClient;
            this.moduleBindContext = moduleBindContext;
            this.metadataContext = metadataContext;
            this.stringHeapCache = stringHeapCache;
            this.eventDescriptorRegistry = eventDescriptorRegistry;
            this.methodDescriptorRegistry = methodDescriptorRegistry;
            this.registeredInjectors = registeredInjectors.ToArray();
            this.serviceProvider = serviceProvider;
            this.logger = loggerFactory.CreateLogger<Instrumentor>();

            instrumentationCache = new();
            emptyResolvedMethodStubs = new();
            codeInjectors = ImmutableDictionary<int, CodeInjector>.Empty;

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

            // Make sure the method is instrumented if necessary
            var (isDirty, unresolvedStubs) = PreprocessMethod(method, moduleInfo, args.Info);
            // Resolve method stubs if necessary
            var resolvedStubs = ResolveStubs(method, moduleInfo, unresolvedStubs, args.Info);
            // Generate new method body if necessary
            var bytecode = GenerateMethodBody(method, isDirty, resolvedStubs);
            // Issue response
            IssueJITCompilationResponse(method, bytecode, args.Info);
        }

        private (bool IsDirty, UnresolvedMethodStubs? Stubs) PreprocessMethod(MethodDef method, ModuleInfo moduleInfo, EventInfo info)
        {
            // Ensure we do not instrument the same method multiple times
            // Also we need to make sure we always return the same bytecode (JITCompilationStarted might be called multiple times for a single method)
            if (!instrumentationCache.TryGetValue(method, out var item))
            {
                lock (method)
                {
                    // Make sure we are the only thread that will be performing the instrumentation
                    if (!instrumentationCache.TryGetValue(method, out item))
                    {
                        // Check if we need to patch any calls to wrapped extern methods
                        var stubs = new UnresolvedMethodStubs();
                        WrapAnalyzedExternMethodCalls(method, moduleInfo, stubs, info);

                        // Check if user requested instrumentation for this method
                        var methodInstrumented = default(bool);
                        if (methodDescriptorRegistry.TryGetMethodInterpretationData(method, out var data))
                        {
                            // Statistics
                            if (data.Flags.HasFlag(MethodRewritingFlags.InjectEntryExitHooks))
                                Interlocked.Increment(ref injectedMethodHooksCount);

                            // Perform instrumenation
                            methodInstrumented = GetCodeInjector(info.ProcessId).TryInject(method, stubs);
                        }

                        // Cache 
                        Guard.True<ArgumentException>(instrumentationCache.TryAdd(method, (methodInstrumented, (stubs.Count != 0) ? stubs : null)));
                        return (methodInstrumented, stubs);
                    }
                }
            }

            return item;
        }


        private void WrapAnalyzedExternMethodCalls(MethodDef method, ModuleInfo moduleInfo, UnresolvedMethodStubs stubs, EventInfo info)
        {
            var resolver = metadataContext.GetResolver(info.ProcessId);
            foreach (var instruction in method.Body.Instructions.Where(i => i.OpCode.Code == Code.Call && i.Operand is IMethodDefOrRef))
            {
                if (resolver.TryResolveMethodDef((instruction.Operand as IMethodDefOrRef)!, out var methodDef))
                {
                    if (methodDef.HasBody)
                    {
                        // This is not an extern method
                        continue;
                    }

                    if (!resolver.TryGetWrapperMethodReference(methodDef, moduleInfo, out _))
                    {
                        // No wrapper registered
                        continue;
                    }
                }
                else
                {
                    // We might need to resolve a method defined within a module that has not been loaded yet
                    // Slow-path: try to match the method based on given reference
                    if (!resolver.TryLookupWrapperMethodReference((instruction.Operand as IMethodDefOrRef)!, moduleInfo, out _))
                    {
                        // No wrapper registered
                        continue;
                    }
                }

                stubs.Add(instruction, HelperOrWrapperReferenceStub.CreateWrapperMethodReferenceStub((instruction.Operand as IMethodDefOrRef)!));
            }
        }

        private ResolvedMethodStubs ResolveStubs(MethodDef methodDef, ModuleInfo moduleInfo, UnresolvedMethodStubs? unresolvedStubs, EventInfo info)
        {
            if (unresolvedStubs is null || unresolvedStubs.Count == 0)
            {
                // Nothing to resolve
                return emptyResolvedMethodStubs;
            }

            var resolver = metadataContext.GetResolver(info.ProcessId);
            var resolvedStubs = new ResolvedMethodStubs();

            foreach (var (instruction, unresolvedStub) in unresolvedStubs)
            {
                MDToken mdToken;

                // Resolve wrapper token
                if (unresolvedStub.IsWrapperMethodReferenceStub())
                {
                    if (resolver.TryResolveMethodDef(unresolvedStub.GetExternMethod(), out var _))
                    {
                        // This must be resolvable
                        resolver.TryGetWrapperMethodReference(methodDef, moduleInfo, out mdToken);
                    }
                    else
                    {
                        // This must be resolvable
                        resolver.TryLookupWrapperMethodReference(unresolvedStub.GetExternMethod(), moduleInfo, out mdToken);
                    }
                }
                // Resolve helper token
                else
                {
                    // This must be resolvable
                    resolver.TryGetHelperMethodReference(unresolvedStub.GetHelperMethodType(), moduleInfo, out mdToken);
                }

                resolvedStubs.Add(instruction, mdToken);
            }

            return resolvedStubs;
        }

        private byte[]? GenerateMethodBody(MethodDef method, bool isDirty, ResolvedMethodStubs stubs)
        {
            // Assemble method body
            var bytecode = default(byte[]);
            if (isDirty || stubs.Count > 0)
            {
                // Statistics
                Interlocked.Increment(ref instrumentedMethodsCount);

                // Instrument method
                var assembler = new FastMethodAssembler(method, stubs, stringHeapCache);
                bytecode = assembler.Assemble();
            }

            return bytecode;
        }

        private void IssueJITCompilationResponse(MethodDef method, byte[]? bytecode, EventInfo info)
        {
            methodDescriptorRegistry.TryGetMethodInterpretationData(method, out var data);

            if (bytecode is not null || data is not null)
            {
                profilingClient.IssueRewriteMethodBodyAsync(bytecode, data, info);
            }
            else
            {
                profilingClient.IssueNoChangesRequestAsync(info);
            }
        }

        private CodeInjector Register(int processId)
        {
            var newInjector = new CodeInjector(processId, registeredInjectors, eventDescriptorRegistry);

            codeInjectors = codeInjectors.Add(processId, newInjector);
            return newInjector;
        }

        private CodeInjector GetCodeInjector(int processId)
        {
            if (!codeInjectors.TryGetValue(processId, out var injector))
            {
                lock (codeInjectors)
                {
                    // Make sure each process gets initialized only once
                    if (!codeInjectors.TryGetValue(processId, out injector))
                        return Register(processId);
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
                    argTypes: id.ArgumentTypes.Select(a =>
                    {
                        if (a.EndsWith('&'))
                        {
                            // Support for by reference types
                            return new ByRefSig(moduleDef.Find(a[0..^1], isReflectionName: true).ToTypeSig());
                        }
                        else
                        {
                            // Common types
                            return moduleDef.Find(a, isReflectionName: true).ToTypeSig();
                        }
                    }).ToArray());

                // Find extern method
                var externMethodDef = declaringTypeDef.FindMethod(id.Name, externMethodSignature);
                Guard.NotNull<MethodDef, ArgumentException>(externMethodDef);

                Interlocked.Increment(ref injectedMethodWrappersCount);
                yield return (new FunctionInfo(moduleId, declaringTypeDef.MDToken, externMethodDef.MDToken), id.ArgsCount);
            }
        }
    }
}
