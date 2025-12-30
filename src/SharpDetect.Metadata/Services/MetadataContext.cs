// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using System.Collections.Immutable;

namespace SharpDetect.Metadata.Services;

internal class MetadataContext : IMetadataContext
{
    private readonly IModuleBindContext _moduleBindContext;
    private ImmutableDictionary<uint, IMetadataEmitter> _emitters;
    private ImmutableDictionary<uint, IMetadataResolver> _resolvers;
    private readonly IServiceProvider _serviceProvider;

    public MetadataContext(
        IModuleBindContext moduleBindContext,
        IServiceProvider serviceProvider)
    {
        _moduleBindContext = moduleBindContext;
        _serviceProvider = serviceProvider;
        _emitters = ImmutableDictionary<uint, IMetadataEmitter>.Empty;
        _resolvers = ImmutableDictionary<uint, IMetadataResolver>.Empty;
    }

    public IMetadataEmitter GetEmitter(uint processId)
    {
        if (_emitters.TryGetValue(processId, out var emitter))
            return emitter;

        CreateContextForProcess(processId);
        return _emitters[processId];
    }

    public IMetadataResolver GetResolver(uint processId)
    {
        if (_resolvers.TryGetValue(processId, out var resolver))
            return resolver;

        CreateContextForProcess(processId);
        return _resolvers[processId];
    }

    private void CreateContextForProcess(uint pid)
    {
        var context = new InjectedData(pid);
        var emitter = ActivatorUtilities.CreateInstance<MetadataEmitter>(_serviceProvider, [pid, context]);
        var resolver = ActivatorUtilities.CreateInstance<MetadataResolver>(_serviceProvider, [pid, _moduleBindContext, context]);

        _emitters = _emitters.Add(pid, emitter);
        _resolvers = _resolvers.Add(pid, resolver);
    }
}
