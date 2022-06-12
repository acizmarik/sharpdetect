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

        private void OnProfilingInitializedHandler((Version? Version, EventInfo Info) args)
        {
            // Create new metadata context
            var context = new InjectedData(args.Info.ProcessId);
            var emitter = new MetadataEmitter(args.Info.ProcessId, context);
            var resolver = new MetadataResolver(args.Info.ProcessId, moduleBindContext, context);

            resolvers = resolvers.Add(args.Info.ProcessId, resolver);
            emitters = emitters.Add(args.Info.ProcessId, emitter);
        }

        private void OnProfilingDestroyedHandler(EventInfo info)
        {
            // Destroy metadata context
            resolvers.Remove(info.ProcessId);
            emitters.Remove(info.ProcessId);
        }
    }
}
