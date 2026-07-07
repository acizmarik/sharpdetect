// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Metadata;
using SharpDetect.Core.Reporting;
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

    public static string GetDisplayMethodName(string metadataName)
    {
        var name = metadataName;
        var doubleColonIndex = name.IndexOf("::", StringComparison.Ordinal);
        var spaceIndex = name.IndexOf(' ');
        if (spaceIndex >= 0 && spaceIndex < doubleColonIndex)
            name = name[(spaceIndex + 1)..];

        return name.Replace("::", ".").Replace('/', '.');
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
            ? GetDisplayMethodName(methodResolveResult.Value.FullName)
            : $"<unresolved-method>({top.MethodToken.Value})";
        var instruction = methodResolveResult.IsSuccess
            ? methodResolveResult.Value.Body.Instructions
                .SingleOrDefault(instr => instr.Offset == access.MethodOffset)?
                .ToString()
            : null;
        instruction ??= $"<unresolved-instruction>(IL_{access.MethodOffset:X4})";
        var symbolInfo = symbolResolver.ResolveSequencePoint(
            processId,
            top.ModuleId,
            top.MethodToken.Value,
            access.MethodOffset);
        var sourceCode = TryReadSourceLine(symbolInfo?.DocumentUrl, symbolInfo?.StartLine);

        return new StackFrame(
            MethodName: methodName,
            SourceMapping: moduleName,
            MethodToken: top.MethodToken.Value,
            MethodOffset: access.MethodOffset,
            Instruction: instruction,
            SourceFileName: symbolInfo?.DocumentUrl,
            SourceLine: symbolInfo?.StartLine,
            SourceCode: sourceCode);
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
            frame.MethodToken,
            GetDisplayMethodName)));

        return frames;
    }

    private static string? TryReadSourceLine(string? documentUrl, int? line)
    {
        if (documentUrl is null || line is null || line.Value < 1)
            return null;

        try
        {
            if (!File.Exists(documentUrl))
                return null;

            return File.ReadLines(documentUrl)
                .Skip(line.Value - 1)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
