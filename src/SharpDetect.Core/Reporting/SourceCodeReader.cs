// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Security.Cryptography;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Reporting;

public static class SourceCodeReader
{
    private const int WindowRadius = 1;
    private static readonly Guid Sha1 = new("ff1816ec-aa5e-4d10-87f7-6f4963833460");
    private static readonly Guid Sha256 = new("8829d00f-11b8-4213-878b-770e8597ac16");
    private static readonly Guid Md5 = new("406ea660-64cf-4c82-b6f0-42d48172a799");
    private static readonly ConcurrentDictionary<(string Path, Guid Algorithm), ImmutableArray<byte>> HashCache = new();
    private static readonly FrozenDictionary<Guid, Func<Stream, byte[]>> HashAlgorithms =
        new Dictionary<Guid, Func<Stream, byte[]>>
        {
            [Sha1] = SHA1.HashData,
            [Sha256] = SHA256.HashData,
            [Md5] = MD5.HashData
        }.ToFrozenDictionary();
    
    public static SourceCodeSnippet TryRead(
        string documentPath,
        int line,
        Guid expectedHashAlgorithm,
        ImmutableArray<byte> expectedHash)
    {
        if (line < 1 || !File.Exists(documentPath))
            return SourceCodeSnippet.None;

        if (MatchesBuild(documentPath, expectedHashAlgorithm, expectedHash) is false)
            return SourceCodeSnippet.OutOfDate;

        try
        {
            var lines = ReadWindow(documentPath, line);
            return !lines.IsEmpty
                ? new SourceCodeSnippet(lines, isOutOfDate: false)
                : SourceCodeSnippet.None;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return SourceCodeSnippet.None;
        }
    }
    
    private static bool? MatchesBuild(string documentPath, Guid expectedHashAlgorithm, ImmutableArray<byte> expectedHash)
    {
        if (expectedHash.IsDefaultOrEmpty || !HashAlgorithms.ContainsKey(expectedHashAlgorithm))
            return null;

        var actualHash = HashCache.GetOrAdd(
            (documentPath, expectedHashAlgorithm),
            static key => ComputeHash(key.Path, key.Algorithm));

        return !actualHash.IsDefaultOrEmpty
            ? actualHash.AsSpan().SequenceEqual(expectedHash.AsSpan())
            : null;
    }

    private static ImmutableArray<byte> ComputeHash(string path, Guid algorithm)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return [.. HashAlgorithms[algorithm](stream)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return ImmutableArray<byte>.Empty;
        }
    }

    private static ImmutableArray<SourceCodeLine> ReadWindow(string documentPath, int line)
    {
        var first = Math.Max(1, line - WindowRadius);
        var last = line + WindowRadius;

        var window = File.ReadLines(documentPath)
            .Skip(first - 1)
            .Take(last - first + 1)
            .Select((text, index) => new SourceCodeLine(first + index, text, IsHighlighted: first + index == line))
            .ToImmutableArray();

        return window.Length > line - first
            ? window
            : ImmutableArray<SourceCodeLine>.Empty;
    }
}
