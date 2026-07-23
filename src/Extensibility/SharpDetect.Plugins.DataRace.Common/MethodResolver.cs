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

    public bool IsStaticConstructorOf(
        uint processId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        TypeDef declaringType)
    {
        if (!TryResolve(processId, moduleId, methodToken, out var methodDef))
            return false;

        if (!methodDef!.IsStaticConstructor)
            return false;

        var comparer = new SigComparer();
        return comparer.Equals(methodDef.DeclaringType, declaringType);
    }

    public bool IsInstanceConstructorOf(
        uint processId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        TypeDef declaringType)
    {
        if (!TryResolve(processId, moduleId, methodToken, out var methodDef))
            return false;

        if (!methodDef!.IsInstanceConstructor)
            return false;

        var comparer = new SigComparer();
        ITypeDefOrRef? current = methodDef.DeclaringType;
        while (current != null)
        {
            if (comparer.Equals(current, declaringType))
                return true;

            current = current.ResolveTypeDef()?.BaseType;
        }

        return false;
    }

    private bool TryResolve(uint processId, ModuleId moduleId, MdMethodDef methodToken, out MethodDef? methodDef)
    {
        var key = new MethodDefOrRef(processId, moduleId, methodToken);
        if (_resolvedMethods.TryGetValue(key, out var cached))
        {
            methodDef = cached;
            return true;
        }

        var resolver = metadataContext.GetResolver(processId);
        var resolveResult = resolver.ResolveMethod(processId, moduleId, methodToken);

        if (resolveResult.IsError)
        {
            logger.LogWarning(
                "Could not resolve method with token={MethodToken} in module {ModuleId}",
                methodToken.Value,
                moduleId.Value);
            methodDef = null;
            return false;
        }

        methodDef = resolveResult.Value;
        _resolvedMethods.Add(key, methodDef);
        return true;
    }
}
