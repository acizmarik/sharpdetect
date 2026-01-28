// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Reporting;
using SharpDetect.Reporting.Services;

namespace SharpDetect.Reporting;

public static class ServiceCollectionExtensions
{
    public static void AddSharpDetectReportingServices(this IServiceCollection services)
    {
        var rootFolder = Path.GetDirectoryName(typeof(ServiceCollectionExtensions).Assembly.Location)!;
        var partialsFolder = Path.Combine(rootFolder, "Templates/Partials");

        services.AddSingleton<IReportSummaryRenderer>(p => new HtmlReportRenderer(new DirectoryInfo(partialsFolder)));
        services.AddSingleton<IReportSummaryWriter, ReportWriter>();
    }
}
