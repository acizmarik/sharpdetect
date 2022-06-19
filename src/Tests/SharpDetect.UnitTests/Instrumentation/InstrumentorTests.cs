using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Instrumentation;
using SharpDetect.Common.Messages;
using SharpDetect.Instrumentation.Injectors;
using Xunit;

namespace SharpDetect.UnitTests.Instrumentation
{
    public class InstrumentorTests : InstrumentorTestsBase
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InstrumentorTests_EnableAndDisableInstrumentationGlobally(bool isEnabled)
        {
            // Prepare
            var context = CreateInstrumentor(isEnabled, InstrumentationStrategy.OnlyPatterns, new[] { nameof(InstrumentorTestTarget) }, new[] { typeof(FieldEventsInjector) });
            var processId = 123;
            var threadId = 456ul;
            var moduleId = 789ul;
            var coreLibModuleId = 101ul;
            var modulePath = typeof(InstrumentorTestTarget).Assembly.Location;
            var coreLibModulePath = typeof(object).Assembly.Location;
            var coreLibModuleDef = AssemblyDef.Load(coreLibModulePath).ManifestModule;
            var typeToken = typeof(InstrumentorTestTarget).MetadataToken;
            var functionToken = typeof(InstrumentorTestTarget).GetMethod(nameof(InstrumentorTestTarget.StaticMethodAccessingStaticValueField))!.MetadataToken;
            var helperTypeToken = 202u;
            var helperMethodToken = 303u;
            var helperFieldInstanceAccessMethodToken = 404u;


            // Act
            context.ProfilingMessageHub.Process(new NotifyMessage() { ProfilerInitialized = new Notify_ProfilerInitialized(), ProcessId = processId, ThreadId = threadId });
            var shadowCLR = context.CreateShadowCLR(processId);
            // ModuleLoaded("System.Private.CoreLib.dll")
            context.EventsHub.RaiseModuleLoaded(null!, new ModuleInfo(new(coreLibModuleId)), coreLibModulePath, new(1, processId, new(threadId)));
            // TypeInjected("EventsHub")
            shadowCLR.Process_TypeInjected(new TypeInfo(new(coreLibModuleId), new(helperTypeToken)));
            // MethodInjected("EventsHub.FieldInstanceAccess")
            shadowCLR.Process_MethodInjected(new FunctionInfo(new(coreLibModuleId), new(helperTypeToken), new(helperFieldInstanceAccessMethodToken)), MethodType.FieldInstanceAccess);
            // MethodInjected("EventsHub.FieldAccess")
            shadowCLR.Process_MethodInjected(new FunctionInfo(new(coreLibModuleId), new(helperTypeToken), new(helperMethodToken)), MethodType.FieldAccess);
            // ModuleLoaded("SharpDetect.UnitTests")
            context.EventsHub.RaiseModuleLoaded(null!, new ModuleInfo(new(moduleId)), modulePath, new(2, processId, new(threadId)));

            var noChangesRequest = false;
            var instrumentRequest = false;
            context.ProfilingClient.IssuedRewriteMethodBodyRequest += (_, _, _) => instrumentRequest = true;
            context.ProfilingClient.IssuedNoChangesRequest += (_) => noChangesRequest = true;

            // HelperReferenced("FieldAccess")
            shadowCLR.Process_HelperMethodReferenced(new(new(moduleId), new(helperTypeToken), new(helperMethodToken)), MethodType.FieldAccess);
            // HelperReferenced("FieldInstanceAccess")
            shadowCLR.Process_HelperMethodReferenced(new(new(moduleId), new(helperTypeToken), new(helperFieldInstanceAccessMethodToken)), MethodType.FieldInstanceAccess);
            // JITCompilationStarted("System.Void SharpDetect.UnitTests.Instrumentation.InstrumentatorTestTarget.StaticMethodAccessingStaticValueField")
            context.EventsHub.RaiseJITCompilationStarted(null!, new FunctionInfo(new(moduleId), new(typeToken), new(functionToken)), new(3, processId, new(threadId)));


            // Assert
            Assert.Equal(isEnabled, instrumentRequest);
            Assert.NotEqual(isEnabled, noChangesRequest);
        }

        [Theory]
        [InlineData(InstrumentationStrategy.OnlyPatterns, new[] { nameof(InstrumentorTestTarget.InstanceMethodAccessingInstanceValueField) }, 2, 1)]
        [InlineData(InstrumentationStrategy.AllExcludingPatterns, new[] { nameof(InstrumentorTestTarget.InstanceMethodAccessingInstanceValueField) }, 1, 2)]
        public void InstrumentorTests_InstrumentOnlyPatterns(InstrumentationStrategy strategy, string[] patterns, int skipped, int instrumented)
        {
            // Prepare
            var context = CreateInstrumentor(true, strategy, patterns, new[] { typeof(FieldEventsInjector) });
            var processId = 123;
            var threadId = 456ul;
            var moduleId = 789ul;
            var coreLibModuleId = 101ul;
            var modulePath = typeof(InstrumentorTestTarget).Assembly.Location;
            var coreLibModulePath = typeof(object).Assembly.Location;
            var coreLibModuleDef = AssemblyDef.Load(coreLibModulePath).ManifestModule;
            var typeToken = typeof(InstrumentorTestTarget).MetadataToken;
            var functionToken1 = typeof(InstrumentorTestTarget).GetMethod(nameof(InstrumentorTestTarget.InstanceMethodAccessingInstanceValueField))!.MetadataToken;
            var functionToken2 = typeof(InstrumentorTestTarget).GetMethod(nameof(InstrumentorTestTarget.InstanceMethodAccessingStaticValueField))!.MetadataToken;
            var functionToken3 = typeof(InstrumentorTestTarget).GetMethod(nameof(InstrumentorTestTarget.StaticMethodAccessingStaticValueField))!.MetadataToken;
            var helperTypeToken = 202u;
            var helperFieldAccessMethodToken = 303u;
            var helperFieldInstanceAccessMethodToken = 404u;


            // Act
            context.ProfilingMessageHub.Process(new NotifyMessage() { ProfilerInitialized = new Notify_ProfilerInitialized(), ProcessId = processId, ThreadId = threadId });
            var shadowCLR = context.CreateShadowCLR(processId);
            // ModuleLoaded("System.Private.CoreLib.dll")
            context.EventsHub.RaiseModuleLoaded(null!, new ModuleInfo(new(coreLibModuleId)), coreLibModulePath, new(1, processId, new(threadId)));
            // TypeInjected("EventsHub")
            shadowCLR.Process_TypeInjected(new TypeInfo(new(coreLibModuleId), new(helperTypeToken)));
            // MethodInjected("EventsHub.FieldAccess")
            shadowCLR.Process_MethodInjected(new FunctionInfo(new(coreLibModuleId), new(helperTypeToken), new(helperFieldAccessMethodToken)), MethodType.FieldAccess);
            // MethodInjected("EventsHub.FieldInstanceAccess")
            shadowCLR.Process_MethodInjected(new FunctionInfo(new(coreLibModuleId), new(helperTypeToken), new(helperFieldInstanceAccessMethodToken)), MethodType.FieldInstanceAccess);
            // ModuleLoaded("SharpDetect.UnitTests")
            context.EventsHub.RaiseModuleLoaded(null!, new ModuleInfo(new(moduleId)), modulePath, new(2, processId, new(threadId)));

            var noChangesRequest = 0;
            var instrumentRequest = 0;
            context.ProfilingClient.IssuedRewriteMethodBodyRequest += (_, _, _) => instrumentRequest++;
            context.ProfilingClient.IssuedNoChangesRequest += (_) => noChangesRequest++;

            // HelperReferenced("FieldAccess")
            shadowCLR.Process_HelperMethodReferenced(new(new(moduleId), new(helperTypeToken), new(helperFieldAccessMethodToken)), MethodType.FieldAccess);
            // HelperReferenced("FieldInstanceAccess")
            shadowCLR.Process_HelperMethodReferenced(new(new(moduleId), new(helperTypeToken), new(helperFieldInstanceAccessMethodToken)), MethodType.FieldInstanceAccess);
            // JITCompilationStarted("method1")
            context.EventsHub.RaiseJITCompilationStarted(null!, new FunctionInfo(new(moduleId), new(typeToken), new(functionToken1)), new(2, processId, new(threadId)));
            // JITCompilationStarted("method2")
            context.EventsHub.RaiseJITCompilationStarted(null!, new FunctionInfo(new(moduleId), new(typeToken), new(functionToken2)), new(3, processId, new(threadId)));
            // JITCompilationStarted("method3")
            context.EventsHub.RaiseJITCompilationStarted(null!, new FunctionInfo(new(moduleId), new(typeToken), new(functionToken3)), new(4, processId, new(threadId)));


            // Assert
            Assert.Equal(instrumented, instrumentRequest);
            Assert.Equal(skipped, noChangesRequest);
        }


        public class InstrumentorTestTarget
        {
            public int StaticValueField;
            public int InstanceValueField;

            public void InstanceMethodAccessingInstanceValueField()
            {
                var cpy = InstanceValueField;
            }

            public void InstanceMethodAccessingStaticValueField()
            {
                var cpy = StaticValueField;
            }

            public void StaticMethodAccessingStaticValueField()
            {
                var cpy = StaticValueField;
            }
        }
    }
}
