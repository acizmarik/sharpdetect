// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;
using System.Collections.Immutable;

namespace SharpDetect.Core.Reporting.Model;

public class Summary
{
    public readonly string Title;
    public readonly string Description;
    public readonly RuntimeInfo RuntimeInfo;
    public readonly RewritingInfo RewritingInfo;
    public readonly TimingInfo TimingInfo;
    public readonly EnvironmentInfo EnvironmentInfo;
    private readonly ImmutableArray<Report> _reports;
    private readonly ImmutableArray<(string, string)> _collectionProperties;
    private readonly ImmutableArray<(string, string)> _runtimeProperties;
    private readonly ImmutableArray<ModuleInfo> _modules;

    public Summary(
        string title,
        string description,
        RuntimeInfo runtimeInfo,
        RewritingInfo rewritingInfo,
        TimingInfo timingInfo,
        EnvironmentInfo environmentInfo,
        IEnumerable<(string, string)> runtimeProps,
        IEnumerable<(string, string)> collectionProps,
        IEnumerable<ModuleInfo> modules,
        IEnumerable<Report> reports)
    {
        Title = title;
        Description = description;
        RuntimeInfo = runtimeInfo;
        RewritingInfo = rewritingInfo;
        TimingInfo = timingInfo;
        EnvironmentInfo = environmentInfo;
        _runtimeProperties = [..runtimeProps];
        _collectionProperties = [..collectionProps];
        _modules = [..modules];
        _reports = [..reports];

    }

    public ImmutableArray<(string Key, string Value)> GetRuntimeProperties()
        => _runtimeProperties;

    public ImmutableArray<(string Key, string Value)> GetCollectionProperties()
        => _collectionProperties;

    public IEnumerable<ModuleInfo> GetAllModules()
        => _modules;

    public IEnumerable<Report> GetAllReports()
        => _reports;
}
