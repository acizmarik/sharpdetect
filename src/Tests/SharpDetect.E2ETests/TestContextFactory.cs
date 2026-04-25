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
        RunCommandArgs args,
        TestPluginAdditionalData additionalData,
        ITestOutputHelper output,
        TimeProvider? timeProvider = null)
    {
        var pluginType = Type.GetType(args.Analysis.PluginFullTypeName!)
                         ?? typeof(Plugins.Deadlock.DeadlockInfo).Assembly.GetType(args.Analysis.PluginFullTypeName!)
                         ?? throw new InvalidOperationException($"Could not find analysis plugin type {args.Analysis.PluginFullTypeName}.");

        return new TestDisposableServiceProvider(new AnalysisServiceProviderBuilder(args)
            .WithTimeProvider(timeProvider ?? TimeProvider.System)
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

    public static TestDisposableServiceProvider CreateServiceProvider(
        string filename,
        string sdk,
        TestPluginAdditionalData additionalData,
        ITestOutputHelper output)
    {
        return CreateServiceProvider(filename, sdk, overridePluginTypeFullName: null, additionalData, output, TimeProvider.System);
    }
    
    public static TestDisposableServiceProvider CreateServiceProvider(
        string filename,
        string sdk,
        string? overridePluginTypeFullName,
        TestPluginAdditionalData additionalData,
        ITestOutputHelper output)
    {
        return CreateServiceProvider(filename, sdk, overridePluginTypeFullName, additionalData, output, TimeProvider.System);
    }
    
    public static TestDisposableServiceProvider CreateServiceProvider(
        string filename,
        string sdk,
        string? overridePluginTypeFullName,
        TestPluginAdditionalData additionalData,
        ITestOutputHelper output,
        TimeProvider timeProvider)
    {
        #if DEBUG
        var buildConfiguration = "Debug";
        #elif RELEASE
        var buildConfiguration = "Release";
        #else
        #error Unknown build configuration. Expected DEBUG or RELEASE.
        #endif

        var config = File.ReadAllText(filename);
        var args = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(config);
        args = args with
        {
            Target = new TargetConfigurationArgs(
                args.Target.Path
                    .Replace("%SDK%", sdk)
                    .Replace("%BUILD_CONFIGURATION%", buildConfiguration),
                args.Target.Args,
                args.Target.WorkingDirectory,
                args.Target.AdditionalEnvironmentVariables,
                args.Target.RedirectInputOutput),
            Analysis = new AnalysisPluginConfigurationArgs(
                args.Analysis.Configuration,
                overridePluginTypeFullName ?? args.Analysis.PluginFullTypeName,
                args.Analysis.PluginName,
                args.Analysis.Path,
                args.Analysis.RenderReport,
                args.Analysis.LogLevel,
                args.Analysis.TemporaryFilesFolder,
                args.Analysis.ReportsFolder,
                args.Analysis.ReportFileName)
        };
        
        var pluginType = Type.GetType(args.Analysis.PluginFullTypeName!)
                         ?? typeof(Plugins.Deadlock.DeadlockInfo).Assembly.GetType(args.Analysis.PluginFullTypeName!)
                         ?? throw new InvalidOperationException($"Could not find analysis plugin type {args.Analysis.PluginFullTypeName}.");
            
        return new TestDisposableServiceProvider(new AnalysisServiceProviderBuilder(args)
            .WithTimeProvider(timeProvider)
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