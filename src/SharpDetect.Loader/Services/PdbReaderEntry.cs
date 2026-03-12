// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace SharpDetect.Loader.Services;

internal sealed class PdbReaderEntry : IDisposable
{
    public MetadataReader Reader { get; }
    private readonly PEReader _peReader;
    private readonly Stream _peStream;
    private readonly MetadataReaderProvider _provider;

    public PdbReaderEntry(
        PEReader peReader,
        Stream peStream,
        MetadataReaderProvider provider,
        MetadataReader reader)
    {
        _peReader = peReader;
        _peStream = peStream;
        _provider = provider;
        Reader = reader;
    }

    public void Dispose()
    {
        _provider.Dispose();
        _peReader.Dispose();
        _peStream.Dispose();
    }
}