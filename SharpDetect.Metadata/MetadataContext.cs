using SharpDetect.Common;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Metadata;
using System.Collections.Immutable;

namespace SharpDetect.Metadata
{
    internal class MetadataContext : IMetadataContext
    {
        private readonly IModuleBindContext moduleBindContext;
        private ImmutableDictionary<int, IMetadataEmitter> emitters;
        private ImmutableDictionary<int, IMetadataResolver> resolvers;

        public MetadataContext(IModuleBindContext moduleBindContext, IProfilingMessageHub profilingMessageHub)
        {
            this.moduleBindContext = moduleBindContext;
            this.emitters = ImmutableDictionary<int, IMetadataEmitter>.Empty;
            this.resolvers = ImmutableDictionary<int, IMetadataResolver>.Empty;

            profilingMessageHub.ProfilerInitialized += OnProfilingInitializedHandler;
            profilingMessageHub.ProfilerDestroyed += OnProfilingDestroyedHandler;
        }

        public IMetadataEmitter GetEmitter(int processId)
            => emitters[processId];

        public IMetadataResolver GetResolver(int processId)
            => resolvers[processId];

        private void OnProfilingInitializedHandler(EventInfo info)
        {
            // Create new metadata context
            var context = new InjectedData(info.ProcessId);
            var emitter = new MetadataEmitter(info.ProcessId, context);
            var resolver = new MetadataResolver(info.ProcessId, moduleBindContext, context);

            resolvers = resolvers.Add(info.ProcessId, resolver);
            emitters = emitters.Add(info.ProcessId, emitter);
        }

        private void OnProfilingDestroyedHandler(EventInfo info)
        {
            // Destroy metadata context
            resolvers.Remove(info.ProcessId);
            emitters.Remove(info.ProcessId);
        }
    }
}
