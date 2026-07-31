// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Commands;
using SharpDetect.Core.Serialization;
using SharpDetect.Serialization.Formatters;

namespace SharpDetect.Serialization.Services;

internal sealed class ProfilerCommandSerializerService : IProfilerCommandSerializer
{
    private readonly MessagePackSerializerOptions _serializerOptions;

    public ProfilerCommandSerializerService()
    {
        _serializerOptions = MessagePackSerializerOptions.Standard
            .WithResolver(CompositeFormatResolver.Instance);
    }

    public byte[] Serialize(ProfilerCommand command)
    {
        return MessagePackSerializer.Serialize(command, _serializerOptions);
    }
}