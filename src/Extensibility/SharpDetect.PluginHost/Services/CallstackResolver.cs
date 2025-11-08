// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Reporting.Model;
using System.Collections.Immutable;
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
        {
            var moduleId = frame.ModuleId;
            var methodToken = frame.MethodToken;
            var methodResolveResult = resolver.ResolveMethod(pid, moduleId, methodToken);
            var methodDef = methodResolveResult.Value;
            var methodName = methodDef?.FullName ?? "<unable-to-resolve-method>";
            var modulePath = methodDef?.Module?.Location ?? "<unable-to-resolve-module>";
            resolvedFrames.Add(new StackFrame(
                MethodName: methodName,
                SourceMapping: modulePath,
                MethodToken: methodToken.Value));
        }

        return new StackTrace(threadInfo, resolvedFrames.ToImmutableArray());
    }
}
