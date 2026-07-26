// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using System.Text.Json;

namespace SharpDetect.Cli;

internal static class ExceptionMessages
{
    private const string Separator = " -> ";
    
    public static string Flatten(Exception exception)
    {
        var messages = new List<string>();
        Collect(exception, messages);
        return messages.Count > 0
            ? string.Join(Separator, messages)
            : exception.GetType().Name;
    }

    private static void Collect(Exception exception, List<string> messages)
    {
        switch (exception)
        {
            case AggregateException aggregateException:
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                    Collect(innerException, messages);
                break;

            case TargetInvocationException { InnerException: { } reflectedException }:
                Collect(reflectedException, messages);
                break;

            case JsonException { InnerException: JsonException }:
                Add(exception.Message, messages);
                break;

            default:
                Add(exception.Message, messages);
                if (exception.InnerException is { } causeException)
                    Collect(causeException, messages);
                break;
        }
    }

    private static void Add(string? message, List<string> messages)
    {
        message = message?.Trim();
        if (string.IsNullOrEmpty(message))
            return;

        if (messages.Any(reported => reported.Contains(message, StringComparison.Ordinal)))
            return;

        messages.Add(message);
    }
}
