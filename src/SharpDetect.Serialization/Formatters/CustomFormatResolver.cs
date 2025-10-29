// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace SharpDetect.Serialization.Formatters;

internal sealed class CustomFormatResolver : IFormatterResolver
{
    private static readonly Dictionary<Type, IMessagePackFormatter> _formatters = new()
    {
        { typeof(UIntPtr), new UIntPtrFormatter() }
    };

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        if (!_formatters.TryGetValue(typeof(T), out var formatter) ||
            formatter is not IMessagePackFormatter<T> concreteFormatter)
        {
            return StandardResolver.Instance.GetFormatter<T>();
        }

        return concreteFormatter;
    }
}
