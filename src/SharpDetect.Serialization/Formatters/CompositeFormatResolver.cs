// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace SharpDetect.Serialization.Formatters;

internal sealed class CompositeFormatResolver : IFormatterResolver
{
    public static readonly IFormatterResolver Instance = new CompositeFormatResolver();

    private CompositeFormatResolver()
    {
    }

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        return Cache<T>.Formatter;
    }

    private static class Cache<T>
    {
        public static readonly IMessagePackFormatter<T>? Formatter;

        static Cache()
        {
            Formatter = CustomFormatResolver.Instance.GetFormatter<T>()
                ?? StandardResolver.Instance.GetFormatter<T>();
        }
    }
}
