// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using MessagePack.Formatters;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Serialization.Formatters;

internal sealed class ThreadIdFormatter : IMessagePackFormatter<ThreadId>
{
    public void Serialize(ref MessagePackWriter writer, ThreadId value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value.ToUInt64());
    }

    public ThreadId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new ThreadId(new UIntPtr(reader.ReadUInt64()));
    }
}

internal sealed class ObjectIdFormatter : IMessagePackFormatter<ObjectId>
{
    public void Serialize(ref MessagePackWriter writer, ObjectId value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value.ToUInt64());
    }

    public ObjectId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new ObjectId(new UIntPtr(reader.ReadUInt64()));
    }
}

internal sealed class ModuleIdFormatter : IMessagePackFormatter<ModuleId>
{
    public void Serialize(ref MessagePackWriter writer, ModuleId value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value.ToUInt64());
    }

    public ModuleId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new ModuleId(new UIntPtr(reader.ReadUInt64()));
    }
}

internal sealed class FunctionIdFormatter : IMessagePackFormatter<FunctionId>
{
    public void Serialize(ref MessagePackWriter writer, FunctionId value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value.ToUInt64());
    }

    public FunctionId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new FunctionId(new UIntPtr(reader.ReadUInt64()));
    }
}

internal sealed class AssemblyIdFormatter : IMessagePackFormatter<AssemblyId>
{
    public void Serialize(ref MessagePackWriter writer, AssemblyId value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value.ToUInt64());
    }

    public AssemblyId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new AssemblyId(new UIntPtr(reader.ReadUInt64()));
    }
}

internal sealed class TrackedObjectIdFormatter : IMessagePackFormatter<TrackedObjectId>
{
    public void Serialize(ref MessagePackWriter writer, TrackedObjectId value, MessagePackSerializerOptions options)
    {
        writer.Write(value.Value.ToUInt64());
    }

    public TrackedObjectId Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        return new TrackedObjectId(new UIntPtr(reader.ReadUInt64()));
    }
}

