// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Metadata;
using SharpDetect.Core.Reporting;
using SharpDetect.Core.Reporting.Formatters;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace.Common;

public static class DataRaceStackTraceResolver
{
    public static IReadOnlyList<StackFrame> ResolveFrames(
        uint processId,
        AccessInfo access,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver)
    {
        var frames = new List<StackFrame> { ResolveTopFrame(processId, access, metadataContext, symbolResolver) };
        frames.AddRange(ResolveDeepFrames(processId, access, metadataContext));
        return frames;
    }

    public static bool IsSystemModule(string modulePath)
        => WellKnownModules.IsSystemModule(modulePath);

    private static StackFrame ResolveTopFrame(
        uint processId,
        AccessInfo access,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver)
    {
        var top = access.Stack.Top;
        var resolver = metadataContext.GetResolver(processId);
        var moduleResolveResult = resolver.ResolveModule(processId, top.ModuleId);
        var methodResolveResult = resolver.ResolveMethod(processId, top.ModuleId, top.MethodToken);
        var moduleName = moduleResolveResult.IsSuccess
            ? moduleResolveResult.Value.Location
            : "<unresolved-module>";
        var methodName = methodResolveResult.IsSuccess
            ? MethodFormatter.ToDisplayName(methodResolveResult.Value.FullName)
            : $"<unresolved-method>({top.MethodToken.Value})";
        var instruction = methodResolveResult.IsSuccess
            ? methodResolveResult.Value.Body.Instructions
                .SingleOrDefault(instr => instr.Offset == access.MethodOffset)?
                .ToString()
            : null;
        instruction ??= $"<unresolved-instruction>({InstructionsFormatter.FormatIlOffset(access.MethodOffset)})";
        var symbolInfo = symbolResolver.ResolveSequencePoint(
            processId,
            top.ModuleId,
            top.MethodToken.Value,
            access.MethodOffset);

        SourceLocation? source = symbolInfo is not null
            ? new SourceLocation(
                symbolInfo.DocumentUrl,
                symbolInfo.StartLine,
                SourceCodeReader.TryRead(
                    symbolInfo.DocumentUrl,
                    symbolInfo.StartLine,
                    symbolInfo.DocumentHashAlgorithm,
                    symbolInfo.DocumentHash))
            : null;

        return new StackFrame(
            MethodName: methodName,
            ModulePath: moduleName,
            MethodToken: top.MethodToken,
            Il: new IlLocation(access.MethodOffset, instruction),
            Source: source);
    }

    private static List<StackFrame> ResolveDeepFrames(
        uint processId,
        AccessInfo access,
        IMetadataContext metadataContext)
    {
        var deeperFrames = access.Stack.GetDeeperFrames();
        if (deeperFrames.Count == 0)
            return [];

        var resolver = metadataContext.GetResolver(processId);
        var frames = new List<StackFrame>(deeperFrames.Count);
        frames.AddRange(deeperFrames.Select(frame => StackFrameResolver.ResolveMinimalFrame(
            resolver,
            processId,
            frame.ModuleId,
            frame.MethodToken)));

        return frames;
    }
}
