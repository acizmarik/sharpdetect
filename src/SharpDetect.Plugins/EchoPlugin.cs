using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Common.Services.Metadata;

namespace SharpDetect.Plugins
{
    [PluginExport("Echo", "1.0.0")]
    public class EchoPlugin : IPlugin
    {
        private ILogger<EchoPlugin> logger;
        private IMetadataContext metadataContext;
        private IEventDescriptorRegistry eventRegistry;
        private bool isDisposed;

        public EchoPlugin()
        {
            this.logger = null!;
            this.metadataContext = null!;
            this.eventRegistry = null!;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<EchoPlugin>();
            metadataContext = serviceProvider.GetRequiredService<IMetadataContext>();
            eventRegistry = serviceProvider.GetRequiredService<IEventDescriptorRegistry>();
        }

        public void AnalysisEnded(EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Analysis ended.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void AnalysisStarted(EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Analysis started.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void ArrayElementRead(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Array element read.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void ArrayElementWritten(ulong srcMappingId, IShadowObject instance, int index, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Array element written.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void FieldRead(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var eventDescriptor = eventRegistry.Get(srcMappingId);
            var field = eventDescriptor.Method.Body.Instructions.SingleOrDefault(i => i.Offset == eventDescriptor.InstructionOffset)?.Operand;
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Field read {field}.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), field);
        }

        public void FieldWritten(ulong srcMappingId, IShadowObject? instance, bool isVolatile, EventInfo info)
        {
            var eventDescriptor = eventRegistry.Get(srcMappingId);
            var field = eventDescriptor.Method.Body.Instructions.SingleOrDefault(i => i.Offset == eventDescriptor.InstructionOffset)?.Operand;
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Field written {field}.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), field);
        }

        public void GarbageCollectionFinished(EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Garbage Collection finished.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void GarbageCollectionStarted(EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Garbage Collection started.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void JITCompilationStarted(FunctionInfo method, EventInfo info)
        {
            metadataContext.GetResolver(info.ProcessId).TryGetMethodDef(method, new(method.ModuleId), resolveWrappers: false, out var methodInfo);
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] JIT compilation {method} started.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), methodInfo);
        }

        public void LockAcquireAttempted(IShadowObject instance, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Lock {obj} acquire attempted.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), instance);
        }

        public void LockAcquireReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            if (isSuccess)
                logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Lock {obj} acquire succeeded.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), instance);
            else
                logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Lock {obj} acquire failed.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), instance);
        }

        public void LockReleased(IShadowObject instance, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Lock {obj} released.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), instance);
        }

        public void MethodCalled(FunctionInfo method, IArgumentsList? arguments, EventInfo info)
        {
            metadataContext.GetResolver(info.ProcessId).TryGetMethodDef(method, new(method.ModuleId), true, out var methodInfo);
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Method {method} called.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), methodInfo);
        }

        public void MethodReturned(FunctionInfo method, IValueOrObject? returnValue, IArgumentsList? byRefArguments, EventInfo info)
        {
            metadataContext.GetResolver(info.ProcessId).TryGetMethodDef(method, new(method.ModuleId), true, out var methodInfo);
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Method {method} returned.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), methodInfo);
        }

        public void ModuleLoaded(ModuleInfo module, string path, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Module loaded.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void ObjectPulsed(IShadowObject instance, bool isPulseAll, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Object pulsed.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void ObjectWaitCalled(IShadowObject instance, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Object wait called.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void ObjectWaitReturned(IShadowObject instance, bool isSuccess, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Object wait returned.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void ThreadCreated(UIntPtr threadId, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Thread created.", info.ProcessId, info.ThreadId, nameof(EchoPlugin));
        }

        public void ThreadDestroyed(UIntPtr threadId, EventInfo info)
        {
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Thread {threadId} destroyed.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), threadId);
        }

        public void TypeLoaded(TypeInfo type, EventInfo info)
        {
            metadataContext.GetResolver(info.ProcessId).TryGetTypeDef(type, new(type.ModuleId), out var typeInfo);
            logger.LogInformation("[PID={pid}][TID={tid}][{plugin}] Type {type} loaded.", info.ProcessId, info.ThreadId, nameof(EchoPlugin), typeInfo);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
