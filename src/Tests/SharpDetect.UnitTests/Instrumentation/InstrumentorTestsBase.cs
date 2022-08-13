using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Models;
using SharpDetect.Core.Runtime;
using SharpDetect.Dnlib.Extensions;
using SharpDetect.Instrumentation;
using SharpDetect.Instrumentation.Injectors;
using SharpDetect.Instrumentation.SourceLinks;

namespace SharpDetect.UnitTests.Instrumentation
{
    public class InstrumentorTestsBase : TestsBase
    {
        internal record InstrumentorContext(
            Instrumentor Instrumentor,
            ProfilingMessageHub ProfilingMessageHub,
            RuntimeEventsHub EventsHub,
            MockProfilingClient ProfilingClient, 
            EventDescriptorRegistry EventRegistry,
            MethodDescriptorRegistry MethodsRegistry, 
            IMetadataContext MetadataContext,
            IModuleBindContext ModuleBindContext)
        {
            public ShadowCLR CreateShadowCLR(int processId)
            {
                return new ShadowCLR(processId, MetadataContext.GetResolver(processId), MetadataContext.GetEmitter(processId), ModuleBindContext);
            }
        }

        public class MockProfilingClient : IProfilingClient
        {
            public event Action<EventInfo>? IssuedContinueExecutionRequest;
            public event Action<IEnumerable<(FunctionInfo Function, ushort Argc)>, EventInfo>? IssuedEmitMethodWrappersRequest;
            public event Action<EventInfo>? IssuedNoChangesRequest;
            public event Action<byte[]?, MethodInterpretationData?, bool?, EventInfo>? IssuedRewriteMethodBodyRequest;

            public Task<Response> IssueContinueExecutionRequestAsync(EventInfo info)
            {
                IssuedContinueExecutionRequest?.Invoke(info);
                return Task.FromResult(new Response() { Result = true });
            }

            public Task<Response> IssueEmitMethodWrappersRequestAsync(IEnumerable<(FunctionInfo Function, ushort Argc)> methods, EventInfo info)
            {
                IssuedEmitMethodWrappersRequest?.Invoke(methods, info);
                return Task.FromResult(new Response() { Result = true });
            }

            public Task<Response> IssueNoChangesRequestAsync(EventInfo info)
            {
                IssuedNoChangesRequest?.Invoke(info);
                return Task.FromResult(new Response() { Result = true });
            }

            public Task<Response> IssueRewriteMethodBodyAsync(byte[]? bytecode, MethodInterpretationData? methodData, bool overrideIssueHooks, EventInfo info)
            {
                IssuedRewriteMethodBodyRequest?.Invoke(bytecode, methodData, overrideIssueHooks, info);
                return Task.FromResult(new Response() { Result = true });
            }
        }

        internal InstrumentorContext CreateInstrumentor(
            bool enableInstrumentation, 
            InstrumentationStrategy strategy, 
            string[] patterns, 
            Type[] injectors)
        {
            var configuration = CreateInstrumentorConfiguration(enableInstrumentation, strategy, patterns);
            var runtimeEventsHub = new RuntimeEventsHub();
            var profilingClient = new MockProfilingClient();
            var moduleBindContext = CreateModuleBindContext();
            var profilingMessageHub = new ProfilingMessageHub(LoggerFactory);
            var metadataContext = CreateMetadataContext(moduleBindContext, profilingMessageHub);
            var stringHeapCache = new StringHeapCache();
            var eventDescriptorRegistry = new EventDescriptorRegistry();
            var methodDescriptorRegistry = CreateRegistryForModulesAsync("Modules/system.private.corelib.lua").Result;
            var instrumentor = new Instrumentor(
                configuration,
                runtimeEventsHub,
                profilingClient,
                moduleBindContext,
                metadataContext,
                stringHeapCache,
                eventDescriptorRegistry,
                methodDescriptorRegistry,
                injectors.Select(t =>
                    (t.GetConstructors().First().Invoke(new object[] { moduleBindContext, methodDescriptorRegistry }) as InjectorBase)!).ToArray());

            return new InstrumentorContext(
                instrumentor, 
                profilingMessageHub, 
                runtimeEventsHub, 
                profilingClient, 
                eventDescriptorRegistry, 
                methodDescriptorRegistry,
                metadataContext,
                moduleBindContext);
        }
    }
}
