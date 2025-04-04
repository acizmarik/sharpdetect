// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

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
            runtimeInfo = new
            {
                type = runtimeName,
                version = summary.RuntimeInfo.Version,
                rewritingProperties = new KeyValuePair<string, string>[]
                {
                    new("AnalyzedMethodsCount", summary.RewritingInfo.AnalyzedMethodsCount.ToString()),
                    new("InjectedTypesCount", summary.RewritingInfo.InjectedTypesCount.ToString()),
                    new("InjectedMethodsCount", summary.RewritingInfo.InjectedMethodsCount.ToString()),
                    new("RewrittenMethodsCount", summary.RewritingInfo.RewrittenMethodsCount.ToString()),
                },
                runtimeProperties = summary.GetRuntimeProperties()
                    .OrderBy(kv => kv.Key)
                    .Select(p => new { key = p.Key, value = p.Value }),
                collectionProperties = summary.GetCollectionProperties()
                    .OrderBy(kv => kv.Key)
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
}
