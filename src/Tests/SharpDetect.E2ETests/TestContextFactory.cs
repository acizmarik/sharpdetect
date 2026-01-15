// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using SharpDetect.Worker.Configuration;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

internal static class TestContextFactory
{
    public static TestDisposableServiceProvider CreateServiceProvider(
        string filename,
        string sdk,
        TestPluginAdditionalData additionalData,
        ITestOutputHelper output)
    {
        #if DEBUG
        var buildConfiguration = "Debug";
        #elif RELEASE
        var buildConfiguration = "Release";
        #else
        #error Unknown build configuration. Expected DEBUG or RELEASE.
        #endif
        
        var args = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(filename);
        args = args with
        {
            Target = args.Target with
            {
                Path = args.Target.Path
                    .Replace("%SDK%", sdk)
                    .Replace("%BUILD_CONFIGURATION%", buildConfiguration)
            }
        };
        
        var pluginType = Type.GetType(args.Analysis.FullTypeName)
                         ?? typeof(Plugins.ConcurrencyContext).Assembly.GetType(args.Analysis.FullTypeName)
                         ?? throw new InvalidOperationException($"Could not find analysis plugin type {args.Analysis.FullTypeName}.");
            
        return new TestDisposableServiceProvider(new AnalysisServiceProviderBuilder(args)
            .WithTimeProvider(TimeProvider.System)
            .WithPlugin(pluginType)
            .ConfigureServices(services =>
            {
                services.AddSingleton(additionalData);
            })
            .ConfigureLogging(logging =>
            {
                logging.AddProvider(new XUnitLoggerProvider(output));
                logging.SetMinimumLevel(args.Analysis.LogLevel);
            })
            .Build());
    }
}