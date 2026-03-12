// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;

namespace SharpDetect.Loader.Services;

internal sealed class SymbolResolver : ISymbolResolver, IDisposable
{
    private readonly IModuleBindContext _moduleBindContext;
    private readonly ILogger<SymbolResolver> _logger;
    private readonly Dictionary<string, PdbReaderEntry> _pdbReaders = new();
    private readonly HashSet<string> _failedModules = new();

    public SymbolResolver(
        IModuleBindContext moduleBindContext,
        ILogger<SymbolResolver> logger)
    {
        _moduleBindContext = moduleBindContext;
        _logger = logger;
    }

    public SequencePointInfo? ResolveSequencePoint(uint pid, ModuleId moduleId, int methodToken, uint ilOffset)
    {
        var moduleResult = _moduleBindContext.TryGetModule(pid, moduleId);
        if (moduleResult.IsError)
            return null;

        var modulePath = moduleResult.Value.Location;
        if (string.IsNullOrEmpty(modulePath))
            return null;

        var entry = GetOrLoadPdbReader(modulePath);
        if (entry is null)
            return null;

        return FindSequencePoint(entry.Reader, methodToken, ilOffset);
    }

    private PdbReaderEntry? GetOrLoadPdbReader(string modulePath)
    {
        if (_pdbReaders.TryGetValue(modulePath, out var entry))
            return entry;

        if (_failedModules.Contains(modulePath))
            return null;

        try
        {
            entry = LoadPdbReader(modulePath);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load debug symbols for module: {Path}", modulePath);
            _failedModules.Add(modulePath);
            return null;
        }

        if (entry is null)
        {
            _failedModules.Add(modulePath);
            return null;
        }

        _pdbReaders[modulePath] = entry;
        return entry;
    }

    private static PdbReaderEntry? LoadPdbReader(string modulePath)
    {
        var peStream = File.OpenRead(modulePath);
        var peReader = new PEReader(peStream);

        return TryLoadEmbeddedPdb(peReader, peStream)
            ?? TryLoadExternalPdb(peReader, peStream, modulePath)
            ?? DisposeAndReturnNull(peReader, peStream);
    }

    private static PdbReaderEntry? TryLoadEmbeddedPdb(PEReader peReader, Stream peStream)
    {
        var entry = peReader.ReadDebugDirectory()
            .FirstOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

        if (entry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
            return null;

        var provider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
        return new PdbReaderEntry(peReader, peStream, provider, provider.GetMetadataReader());
    }

    private static PdbReaderEntry? TryLoadExternalPdb(PEReader peReader, Stream peStream, string modulePath)
    {
        var pdbPath = Path.ChangeExtension(modulePath, ".pdb");
        if (!File.Exists(pdbPath))
            return null;

        var pdbStream = File.OpenRead(pdbPath);
        try
        {
            var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            return new PdbReaderEntry(peReader, peStream, provider, provider.GetMetadataReader());
        }
        catch
        {
            // Not a portable PDB
            pdbStream.Dispose();
            return null;
        }
    }

    private static PdbReaderEntry? DisposeAndReturnNull(PEReader peReader, Stream peStream)
    {
        peReader.Dispose();
        peStream.Dispose();
        return null;
    }

    private static SequencePointInfo? FindSequencePoint(MetadataReader reader, int methodToken, uint ilOffset)
    {
        // Strip the table-type byte to get the 1-based method row number
        var rowNumber = methodToken & 0x00FFFFFF;
        if (rowNumber <= 0)
            return null;

        MethodDebugInformation debugInfo;
        try
        {
            debugInfo = reader.GetMethodDebugInformation(MetadataTokens.MethodDebugInformationHandle(rowNumber));
        }
        catch
        {
            return null;
        }

        // Find the last non-hidden sequence point whose offset does not exceed ilOffset
        SequencePoint? bestMatch = null;
        foreach (var sp in debugInfo.GetSequencePoints())
        {
            if (sp.IsHidden || sp.Offset > (int)ilOffset)
                continue;

            if (bestMatch is null || sp.Offset > bestMatch.Value.Offset)
                bestMatch = sp;
        }

        if (bestMatch is null)
            return null;

        var document = reader.GetDocument(bestMatch.Value.Document);
        return new SequencePointInfo(reader.GetString(document.Name), bestMatch.Value.StartLine);
    }

    public void Dispose()
    {
        foreach (var entry in _pdbReaders.Values)
            entry.Dispose();

        _pdbReaders.Clear();
        _failedModules.Clear();
    }
}

