// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Reflection;
using HandlebarsDotNet;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Reporting.Services;

internal sealed class HtmlReportRenderer : IReportSummaryRenderer
{
    private readonly DirectoryInfo _primaryDirectory;
    private readonly string _template;

    public HtmlReportRenderer(DirectoryInfo directory)
    {
        _primaryDirectory = directory;
        var baseTemplateFile = _primaryDirectory.GetFiles("_base.html").Single();
        _template = File.ReadAllText(baseTemplateFile.FullName);
    }

    public string Render(Summary summary, IPlugin plugin, DirectoryInfo additionalPartials)
    {
        var environment = CreateEnvironment(additionalPartials);
        var dataContent = BuildDataContext(plugin, summary);
        var compiledTemplate = environment.Compile(_template);
        return compiledTemplate(dataContent);
    }

    private IHandlebars CreateEnvironment(DirectoryInfo additionalPartials)
    {
        var stylesTemplateFile = _primaryDirectory.GetFiles("styles.css").Single();
        var htmlTemplateFiles = _primaryDirectory.GetFiles("*.html");
        var additionalHtmlTemplateFiles = additionalPartials.GetFiles("*.html");
        var htmlTemplates = htmlTemplateFiles
            .Concat(additionalHtmlTemplateFiles)
            .Concat([stylesTemplateFile])
            .Select(file => (
                Path.GetFileNameWithoutExtension(file.FullName),
                File.ReadAllText(file.FullName)))
            .ToDictionary(kv => kv.Item1, kv => kv.Item2);

        var environment = Handlebars.CreateSharedEnvironment();
        foreach (var (templateName, templateContent) in htmlTemplates)
            environment.RegisterTemplate(templateName, templateContent);

        return environment;
    }

    private static object BuildDataContext(IPlugin plugin, Summary summary)
    {
        var sharpDetectVersion = typeof(HtmlReportRenderer).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion 
                      ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                      ?? "unknown version";
        
        var runtimeName = summary.RuntimeInfo.Type switch
        {
            COR_PRF_RUNTIME_TYPE.COR_PRF_DESKTOP_CLR => "CLR",
            COR_PRF_RUNTIME_TYPE.COR_PRF_CORE_CLR => "CoreCLR",
            _ => "unknown runtime"
        };
        return new
        {
            title = summary.Title,
            description = summary.Description,
            environmentInfo = new
            {
                sharpDetectVersion,
                operatingSystem = summary.EnvironmentInfo.OperatingSystem,
                architecture = summary.EnvironmentInfo.ProcessorArchitecture,
                processorCount = summary.EnvironmentInfo.ProcessorCount,
                totalMemory = FormatBytes(summary.EnvironmentInfo.TotalPhysicalMemoryBytes),
                machineName = summary.EnvironmentInfo.MachineName,
                userName = summary.EnvironmentInfo.UserName,
                workingDirectory = summary.EnvironmentInfo.WorkingDirectory
            },
            runtimeInfo = new
            {
                type = runtimeName,
                version = summary.RuntimeInfo.Version,
                timingInfo = new
                {
                    startTime = summary.TimingInfo.AnalysisStartTime.ToString("o", CultureInfo.InvariantCulture),
                    endTime = summary.TimingInfo.AnalysisEndTime.ToString("o", CultureInfo.InvariantCulture),
                    duration = FormatDuration(summary.TimingInfo.AnalysisDuration),
                    durationSeconds = summary.TimingInfo.AnalysisDuration.TotalSeconds.ToString("F2")
                },
                rewritingProperties = new KeyValuePair<string, string>[]
                {
                    new("Analyzed Methods Count", summary.RewritingInfo.AnalyzedMethodsCount.ToString()),
                    new("Injected Types Count", summary.RewritingInfo.InjectedTypesCount.ToString()),
                    new("Injected Methods Count", summary.RewritingInfo.InjectedMethodsCount.ToString()),
                    new("Rewritten Methods Count", summary.RewritingInfo.RewrittenMethodsCount.ToString()),
                },
                runtimeProperties = summary.GetRuntimeProperties()
                    .Select(p => new { key = p.Key, value = p.Value }),
                collectionProperties = summary.GetCollectionProperties()
                    .Select(kv => new { key = kv.Key, value = kv.Value })
            },
            assemblies = summary.GetAllModules().Select(moduleInfo =>
            {
                return new
                {
                    name = moduleInfo.Name,
                    version = moduleInfo.Version,
                    path = moduleInfo.Path,
                    publicKey = moduleInfo.PublicKey,
                    culture = moduleInfo.Culture
                };
            }),
            reports = plugin.CreateReportDataContext(summary.GetAllReports()).ToArray()
        };
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return $"{duration.TotalMilliseconds:F0}ms";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F2}s";
        if (duration.TotalHours < 1)
            return $"{duration.Minutes}m {duration.Seconds}s";
        return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0)
            return "N/A";
        
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:F2} {sizes[order]}";
    }
}
