// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Worker.Commands.Run;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests.Utils;

internal sealed class E2ETestBuilder
{
    private const string SubjectRelativePath =
        "../../../../SharpDetect.E2ETests.Subject/bin/%BUILD_CONFIGURATION%/%SDK%/SharpDetect.E2ETests.Subject.dll";
    private const string PluginsRelativePath =
        "../../../../../Extensibility/SharpDetect.Plugins/bin/%BUILD_CONFIGURATION%/%SDK%/SharpDetect.Plugins.dll";

#if DEBUG
    private const string BuildConfiguration = "Debug";
#elif RELEASE
    private const string BuildConfiguration = "Release";
#else
#error Unknown build configuration. Expected DEBUG or RELEASE.
#endif

    private readonly string _subjectArgs;
    private string? _pluginTypeName;
    private string? _analysisAssemblyPath;
    private object? _pluginConfiguration;
    private bool _renderReport;
    private KeyValuePair<string, string>[]? _additionalTargetEnvironmentVariables;
    private const ProfilerLogLevel ProfilerLogLevel = Worker.Commands.Run.ProfilerLogLevel.Information;
    private const LogLevel AnalysisLogLevel = LogLevel.Information;

    private E2ETestBuilder(string subjectArgs)
    {
        _subjectArgs = subjectArgs;
    }

    public static E2ETestBuilder ForSubject(string subjectArgs)
    {
        return new E2ETestBuilder(subjectArgs);
    }

    public E2ETestBuilder WithPlugin<TPlugin>() where TPlugin : class
    {
        _pluginTypeName = typeof(TPlugin).FullName
            ?? throw new InvalidOperationException($"Type {typeof(TPlugin)} has no FullName.");
        _analysisAssemblyPath = Path.GetFileName(typeof(TPlugin).Assembly.Location);
        return this;
    }
    
    public E2ETestBuilder WithTestPlugin(string pluginFullTypeName)
    {
        _pluginTypeName = pluginFullTypeName;
        _analysisAssemblyPath = Path.GetFileName(typeof(E2ETestBuilder).Assembly.Location);
        return this;
    }
    
    public E2ETestBuilder WithExternalPlugin(string pluginFullTypeName)
    {
        _pluginTypeName = pluginFullTypeName;
        _analysisAssemblyPath = PluginsRelativePath;
        return this;
    }

    public E2ETestBuilder WithPluginConfiguration(object configuration)
    {
        _pluginConfiguration = configuration;
        return this;
    }

    public E2ETestBuilder WithRenderReport()
    {
        _renderReport = true;
        return this;
    }

    public E2ETestBuilder WithTargetEnvironmentVariables(params KeyValuePair<string, string>[] variables)
    {
        _additionalTargetEnvironmentVariables = variables;
        return this;
    }

    public TestDisposableServiceProvider Build(
        string sdk,
        ITestOutputHelper output,
        TestPluginAdditionalData? additionalData = null,
        TimeProvider? timeProvider = null)
    {
        if (_pluginTypeName is null)
            throw new InvalidOperationException("A plugin must be configured via WithPlugin<T>() or WithExternalPlugin().");

        var sessionId = Guid.NewGuid().ToString("N");
        var temporaryFilesFolder = Path.Combine(Path.GetTempPath(), sessionId);
        Directory.CreateDirectory(temporaryFilesFolder);

        var resolvedSubjectPath = SubjectRelativePath
            .Replace("%SDK%", sdk)
            .Replace("%BUILD_CONFIGURATION%", BuildConfiguration);

        var resolvedAnalysisPath = (_analysisAssemblyPath ?? string.Empty)
            .Replace("%SDK%", sdk)
            .Replace("%BUILD_CONFIGURATION%", BuildConfiguration);

        var args = new RunCommandArgs(
            Runtime: new RuntimeConfigurationArgs(
                Host: null,
                Profiler: new ProfilerConfigurationArgs(
                    logLevel: ProfilerLogLevel)),
            Target: new TargetConfigurationArgs(
                path: resolvedSubjectPath,
                args: _subjectArgs,
                additionalEnvironmentVariables: _additionalTargetEnvironmentVariables,
                redirectInputOutput: new RedirectInputOutputConfigurationArgs(singleConsoleMode: true)),
            Analysis: new AnalysisPluginConfigurationArgs(
                configuration: _pluginConfiguration,
                pluginFullTypeName: _pluginTypeName,
                path: resolvedAnalysisPath,
                renderReport: _renderReport,
                logLevel: AnalysisLogLevel,
                temporaryFilesFolder: temporaryFilesFolder,
                sessionId: sessionId));

        return TestContextFactory.CreateServiceProvider(
            args,
            additionalData ?? TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled(),
            output,
            timeProvider ?? TimeProvider.System);
    }
}
