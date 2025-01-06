// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Serialization.Services;

namespace SharpDetect.Serialization.Configuration;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectSerializationServices(this IServiceCollection services)
    {
        services.AddSingleton<IRecordedEventParser, RecordedEventParserService>();
        services.AddSingleton<IArgumentsParser, ArgumentsParserService>();
    }
}
