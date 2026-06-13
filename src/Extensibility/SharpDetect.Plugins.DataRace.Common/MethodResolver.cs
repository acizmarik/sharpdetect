// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpDetect.Plugins.DataRace.Common;

public sealed class MethodResolver(IMetadataContext metadataContext, ILogger logger)
{
    private readonly Dictionary<MethodDefOrRef, MethodDef> _resolvedMethods = [];

    public ConstructorKind GetConstructorKind(uint processId, ModuleId moduleId, MdMethodDef methodToken)
    {
        var key = new MethodDefOrRef(processId, moduleId, methodToken);
        if (_resolvedMethods.TryGetValue(key, out var methodDef))
            return GetConstructorKind(methodDef);

        var resolver = metadataContext.GetResolver(processId);
        var resolveResult = resolver.ResolveMethod(processId, moduleId, methodToken);

        if (resolveResult.IsError)
        {
            logger.LogWarning(
                "Could not resolve method with token={MethodToken} in module {ModuleId}",
                methodToken.Value,
                moduleId.Value);
            return ConstructorKind.None;
        }

        methodDef = resolveResult.Value;
        _resolvedMethods.Add(key, methodDef);
        return GetConstructorKind(methodDef);
    }

    private static ConstructorKind GetConstructorKind(MethodDef methodDef)
    {
        if (!methodDef.IsConstructor)
            return ConstructorKind.None;

        return methodDef.IsInstanceConstructor ? ConstructorKind.Instance : ConstructorKind.Static;
    }
}
