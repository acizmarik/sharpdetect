// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace SharpDetect.PluginHost.Services;

public class RecordedEventBindingsCompiler : IRecordedEventBindingsCompiler
{
    public ImmutableDictionary<RecordedEventHandlerType, BoundMethodEnterExitHandler> CompileCustomEventBindings(IPlugin plugin)
    {
        var builder = ImmutableDictionary.CreateBuilder<RecordedEventHandlerType, BoundMethodEnterExitHandler>();
        foreach (var method in plugin.GetType().GetMethods())
        {
            var customAttributes = method.GetCustomAttributesData();
            var eventBindAttribute = customAttributes.FirstOrDefault(a => a.AttributeType == typeof(RecordedEventBindAttribute));
            if (eventBindAttribute == null)
                continue;

            // Get custom event type
            var recordedEventType = (RecordedEventType)(ushort)eventBindAttribute.ConstructorArguments[0]!.Value!;
            var argType = method.GetParameters()[1].ParameterType;

            // Dynamically generate and compile invoker
            var parameterPlugin = Expression.Parameter(typeof(IPlugin), "plugin");
            var parameterMetadata = Expression.Parameter(typeof(RecordedEventMetadata), "metadata");
            var parameterArgs = Expression.Parameter(typeof(IRecordedEventArgs), "args");
            var upcastedPlugin = Expression.Convert(parameterPlugin, plugin.GetType());
            var upcastedArgs = Expression.Convert(parameterArgs, argType);
            var invocation = Expression.Call(upcastedPlugin, method, parameterMetadata, upcastedArgs);
            var lambda = Expression.Lambda<Action<IPlugin, RecordedEventMetadata, IRecordedEventArgs>>
                (invocation, parameterPlugin, parameterMetadata, parameterArgs);
            BoundMethodEnterExitHandler compiled = lambda.Compile(preferInterpretation: false).Invoke;
            builder.Add(new(recordedEventType, argType), compiled);
        }

        return builder.ToImmutable();
    }
}
