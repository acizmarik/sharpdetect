// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using MessagePack.Formatters;

namespace SharpDetect.Serialization.Formatters;

internal sealed class UIntPtrFormatter : IMessagePackFormatter<UIntPtr>
{
    public void Serialize(ref MessagePackWriter writer, UIntPtr value, MessagePackSerializerOptions options)
    {
        writer.Write(value.ToUInt64());
    }

    public UIntPtr Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new UIntPtr(reader.ReadUInt64());
    }
}
