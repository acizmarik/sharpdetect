// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Reporting.Model;
using System.Collections.Immutable;
using SharpDetect.Core.Reporting;
using StackFrame = SharpDetect.Core.Reporting.Model.StackFrame;

namespace SharpDetect.PluginHost.Services;

internal class CallstackResolver : ICallstackResolver
{
    private readonly IMetadataContext _metadataContext;

    public CallstackResolver(IMetadataContext metadataContext)
    {
        _metadataContext = metadataContext;
    }

    public StackTrace Resolve(ThreadInfo threadInfo, Callstack callstack)
    {
        var pid = callstack.ProcessThreadId.ProcessId;
        var resolver = _metadataContext.GetResolver(pid);
        var resolvedFrames = ImmutableArray.CreateBuilder<StackFrame>();
        foreach (var frame in callstack)
            resolvedFrames.Add(StackFrameResolver.ResolveMinimalFrame(resolver, pid, frame.ModuleId, frame.MethodToken));

        return new StackTrace(threadInfo, resolvedFrames.ToImmutableArray());
    }
}
