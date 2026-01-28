// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using MessagePack.Formatters;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Serialization.Formatters;

internal sealed class MdTokenFormatter : IMessagePackFormatter<MdToken>
{
    public void Serialize(ref MessagePackWriter writer, MdToken value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public MdToken Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new MdToken(reader.ReadInt32());
    }
}

internal sealed class MdTypeDefFormatter : IMessagePackFormatter<MdTypeDef>
{
    public void Serialize(ref MessagePackWriter writer, MdTypeDef value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public MdTypeDef Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new MdTypeDef(reader.ReadInt32());
    }
}

internal sealed class MdTypeRefFormatter : IMessagePackFormatter<MdTypeRef>
{
    public void Serialize(ref MessagePackWriter writer, MdTypeRef value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public MdTypeRef Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new MdTypeRef(reader.ReadInt32());
    }
}

internal sealed class MdMethodDefFormatter : IMessagePackFormatter<MdMethodDef>
{
    public void Serialize(ref MessagePackWriter writer, MdMethodDef value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public MdMethodDef Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new MdMethodDef(reader.ReadInt32());
    }
}

internal sealed class MdMemberRefFormatter : IMessagePackFormatter<MdMemberRef>
{
    public void Serialize(ref MessagePackWriter writer, MdMemberRef value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value);
    }

    public MdMemberRef Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new MdMemberRef(reader.ReadInt32());
    }
}

