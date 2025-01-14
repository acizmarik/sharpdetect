// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Reporting;
using SharpDetect.Reporting.Services;

namespace SharpDetect.Reporting;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectReportingServices(this IServiceCollection services)
    {
        services.AddSingleton<IReportSummaryRenderer>(p => new HtmlReportRenderer(new DirectoryInfo("Templates/Partials")));
        services.AddSingleton<IReportSummaryDisplayer, HtmlReportDisplayer>();
    }
}
