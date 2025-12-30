// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Serialization.Formatters;

internal sealed class CustomFormatResolver : IFormatterResolver
{
    public static readonly IFormatterResolver Instance = new CustomFormatResolver();

    private CustomFormatResolver()
    {
    }

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        return FormatterCache<T>.Formatter;
    }

    private static class FormatterCache<T>
    {
        public static readonly IMessagePackFormatter<T>? Formatter;

        static FormatterCache()
        {
            Formatter = (IMessagePackFormatter<T>?)CustomFormatterHelper.GetFormatter(typeof(T));
        }
    }
}

internal static class CustomFormatterHelper
{
    private static readonly Dictionary<Type, object> CustomFormatters = new()
    {
        { typeof(UIntPtr), new UIntPtrFormatter() },
        { typeof(ThreadId), new ThreadIdFormatter() },
        { typeof(ObjectId), new ObjectIdFormatter() },
        { typeof(ModuleId), new ModuleIdFormatter() },
        { typeof(FunctionId), new FunctionIdFormatter() },
        { typeof(AssemblyId), new AssemblyIdFormatter() },
        { typeof(TrackedObjectId), new TrackedObjectIdFormatter() },
        { typeof(MdToken), new MdTokenFormatter() },
        { typeof(MdTypeDef), new MdTypeDefFormatter() },
        { typeof(MdTypeRef), new MdTypeRefFormatter() },
        { typeof(MdMethodDef), new MdMethodDefFormatter() },
        { typeof(MdMemberRef), new MdMemberRefFormatter() }
    };

    internal static object? GetFormatter(Type t)
    {
        if (CustomFormatters.TryGetValue(t, out var formatter))
        {
            return formatter;
        }

        return null;
    }
}
