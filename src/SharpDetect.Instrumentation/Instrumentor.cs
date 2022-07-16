using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Configuration;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Instrumentation;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Dnlib.Extensions;
using SharpDetect.Instrumentation.Injectors;
using SharpDetect.Instrumentation.Stubs;
using SharpDetect.Instrumentation.Utilities;
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
        private InstrumentationOptions options;
        private volatile int instrumentedMethodsCount;
        private volatile int injectedMethodWrappersCount;
        private volatile int injectedMethodHooksCount;

        public int InstrumentedMethodsCount { get => instrumentedMethodsCount; }
        public int InjectedMethodWrappersCount { get => injectedMethodWrappersCount; }
        public int InjectedMethodHooksCount { get => injectedMethodHooksCount; }

        public Instrumentor(
            IConfiguration configuration,
            IShadowExecutionObserver executionObserver,
            IProfilingClient profilingClient,
            IModuleBindContext moduleBindContext,
            IMetadataContext metadataContext,
            IStringHeapCache stringHeapCache,
            IEventDescriptorRegistry eventDescriptorRegistry,
            IMethodDescriptorRegistry methodDescriptorRegistry,
            IEnumerable<InjectorBase> registeredInjectors)
        {
            this.profilingClient = profilingClient;
            this.moduleBindContext = moduleBindContext;
            this.metadataContext = metadataContext;
            this.stringHeapCache = stringHeapCache;
            this.eventDescriptorRegistry = eventDescriptorRegistry;
            this.methodDescriptorRegistry = methodDescriptorRegistry;
            this.registeredInjectors = registeredInjectors.ToArray();

            instrumentationCache = new();
            emptyResolvedMethodStubs = new();
            codeInjectors = ImmutableDictionary<int, CodeInjector>.Empty;

            executionObserver.ModuleLoaded += ExecutionObserver_ModuleLoaded;
            executionObserver.JITCompilationStarted += ExecutionObserver_JITCompilationStarted;

            options = new InstrumentationOptions(
                configuration.GetSection(Constants.Instrumentation.Enabled).Get<bool>(),
                configuration.GetSection(Constants.Instrumentation.Strategy).Get<InstrumentationStrategy>(),
                configuration.GetSection(Constants.Instrumentation.Patterns).Get<string[]>());
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
            if (!resolver.TryGetMethodDef(args.Function, moduleInfo, resolveWrappers: false, out var method))
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
            var resolvedStubs = ResolveStubs(moduleInfo, unresolvedStubs, args.Info);
            // Generate new method body if necessary
            var bytecode = GenerateMethodBody(method, isDirty, resolvedStubs);

            if (MethodsGenerator.IsManagedWrapper(method) && resolver.TryGetExternMethodDefinition(new(args.Function.FunctionToken), moduleInfo, out var externMethod))
            {
                // Library descriptors now nothing about our wrappers
                // Before passing them for analysis, we should resolve them always
                method = externMethod.Method;
            }

            // Statistics
            methodDescriptorRegistry.TryGetMethodInterpretationData(method, out var interpretation);
            if (interpretation is not null && interpretation.Flags.HasFlag(MethodRewritingFlags.InjectEntryExitHooks))
                Interlocked.Increment(ref injectedMethodHooksCount);
            if (bytecode is not null)
                Interlocked.Increment(ref instrumentedMethodsCount);

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
                        
                        var methodInstrumented = default(bool);
                        // Apply actions defined by method descriptors
                        methodDescriptorRegistry.TryGetMethodInterpretationData(method, out var data);
                        // Check if user requested instrumentation for this method
                        if (options.Enabled)
                        {
                            if (options.Strategy == InstrumentationStrategy.OnlyPatterns && options.Patterns.Any(p => method.FullName.Contains(p)))
                            {
                                // Perform instrumenation
                                methodInstrumented = GetCodeInjector(info.ProcessId).TryInject(method, stubs);
                            }
                            else if (options.Strategy == InstrumentationStrategy.AllExcludingPatterns && options.Patterns.All(p => !method.FullName.Contains(p)))
                            {
                                // Perform instrumenation
                                methodInstrumented = GetCodeInjector(info.ProcessId).TryInject(method, stubs);
                            }
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

        private ResolvedMethodStubs ResolveStubs(ModuleInfo moduleInfo, UnresolvedMethodStubs? unresolvedStubs, EventInfo info)
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
                    WrapperMethodRefMDToken wrapperMdToken;
                    if (resolver.TryResolveMethodDef(unresolvedStub.GetExternMethod(), out var externMethodDef))
                    {
                        // This must be resolvable
                        resolver.TryGetWrapperMethodReference(externMethodDef, moduleInfo, out wrapperMdToken);
                    }
                    else
                    {
                        // This must be resolvable
                        resolver.TryLookupWrapperMethodReference(unresolvedStub.GetExternMethod(), moduleInfo, out wrapperMdToken);
                    }
                    mdToken = wrapperMdToken.Token;
                }
                // Resolve helper token
                else
                {
                    // This must be resolvable
                    resolver.TryGetHelperMethodReference(unresolvedStub.GetHelperMethodType(), moduleInfo, out var helperMdToken);
                    mdToken = helperMdToken.Token;
                }

                Guard.NotEqual<MDToken, ArgumentException>(default, mdToken);
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
                // Instrument method
                var assembler = new FastMethodAssembler(method, stubs, stringHeapCache);
                bytecode = assembler.Assemble();
            }

            return bytecode;
        }

        private void IssueJITCompilationResponse(MethodDef method, byte[]? bytecode, EventInfo info)
        {
            var result = methodDescriptorRegistry.TryGetMethodInterpretationData(method, out var data);

            if (result || bytecode is not null)
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
