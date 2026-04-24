// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CommunityToolkit.Diagnostics;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Plugins
{
    public class SummaryBuilder
    {
        private readonly TimeProvider _timeProvider;
        private readonly List<(string, string)> _runtimeProperties;
        private readonly List<(string, string)> _collectionProperties;
        private readonly List<ModuleInfo> _modules;
        private readonly List<Report> _reports;
        private readonly List<(uint Pid, RuntimeInfo RuntimeInfo)> _runtimeInfos;
        private ulong _injectedTypesCount;
        private ulong _injectedMethodsCount;
        private ulong _rewrittenMethodsCount;
        private ulong _analyzedMethodsCount;
        private ulong _garbageCollectionsCount;
        private ulong _methodEnterExitCount;
        private DateTime _startTime;
        private string? _title;
        private string? _description;

        public SummaryBuilder(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
            _startTime = timeProvider.GetUtcNow().DateTime;
            _runtimeProperties = [];
            _collectionProperties = [];
            _modules = [];
            _reports = [];
            _runtimeInfos = [];
        }

        public SummaryBuilder AddReport(Report report)
        {
            _reports.Add(report);
            return this;
        }

        public SummaryBuilder SetTitle(string title)
        {
            _title = title;
            return this;
        }

        public SummaryBuilder SetDescription(string description)
        {
            _description = description;
            return this;
        }

        public SummaryBuilder AddRuntimeInfo(uint processId, RuntimeInfo runtimeInfo)
        {
            _runtimeInfos.Add((processId, runtimeInfo));
            return this;
        }
        
        public SummaryBuilder SetStartTime()
        {
            _startTime = _timeProvider.GetUtcNow().DateTime;
            return this;
        }

        public SummaryBuilder AddRuntimeProperty(string name, string value)
        {
            _runtimeProperties.Add((name, value));
            return this;
        }

        public SummaryBuilder AddCollectionProperty(string name, string value)
        {
            _collectionProperties.Add((name, value));
            return this;
        }

        public SummaryBuilder AddModule(ModuleInfo moduleInfo)
        {
            _modules.Add(moduleInfo);
            return this;
        }

        public SummaryBuilder IncrementAnalyzedMethodsCounter()
        {
            _analyzedMethodsCount++;
            return this;
        }

        public SummaryBuilder IncrementInjectedTypesCounter()
        {
            _injectedTypesCount++;
            return this;
        }

        public SummaryBuilder IncrementInjectedMethodsCounter()
        {
            _injectedMethodsCount++;
            return this;
        }

        public SummaryBuilder IncrementRewrittenMethodsCounter()
        {
            _rewrittenMethodsCount++;
            return this;
        }

        public SummaryBuilder IncrementGarbageCollectionsCounter()
        {
            _garbageCollectionsCount++;
            return this;
        }

        public SummaryBuilder IncrementMethodEnterExitCounter()
        {
            _methodEnterExitCount++;
            return this;
        }

        public Summary Build()
        {
            Guard.IsNotNullOrWhiteSpace(_title);
            Guard.IsNotNullOrWhiteSpace(_description);

            var endTime = DateTime.UtcNow;
            var timingInfo = new TimingInfo(
                AnalysisStartTime: _startTime,
                AnalysisEndTime: endTime,
                AnalysisDuration: endTime - _startTime);

            _collectionProperties.Add(("Garbage Collections Count", _garbageCollectionsCount.ToString()));
            _collectionProperties.Add(("Method Enter/Exit Count", _methodEnterExitCount.ToString()));

            foreach (var (pid, info) in _runtimeInfos)
            {
                var typeName = info.Type switch
                {
                    Events.Profiler.COR_PRF_RUNTIME_TYPE.COR_PRF_DESKTOP_CLR => "CLR",
                    Events.Profiler.COR_PRF_RUNTIME_TYPE.COR_PRF_CORE_CLR => "CoreCLR",
                    _ => "unknown"
                };
                _runtimeProperties.Add(($"Runtime (PID {pid})", $"{typeName} {info.Version}"));
            }

            return new Summary(
                title: _title,
                description: _description,
                rewritingInfo: new RewritingInfo(
                    _analyzedMethodsCount,
                    _injectedTypesCount,
                    _injectedMethodsCount,
                    _rewrittenMethodsCount),
                timingInfo: timingInfo,
                environmentInfo: CaptureEnvironmentInfo(),
                runtimeProps: _runtimeProperties,
                collectionProps: _collectionProperties,
                modules: _modules,
                reports: _reports);
        }

        private static EnvironmentInfo CaptureEnvironmentInfo()
        {
            var osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
            var processorCount = Environment.ProcessorCount;
            
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcMemoryInfo.TotalAvailableMemoryBytes;

            var machineName = Environment.MachineName;
            var userName = Environment.UserName;
            var workingDirectory = Environment.CurrentDirectory;

            return new EnvironmentInfo(
                OperatingSystem: osDescription,
                ProcessorArchitecture: architecture,
                ProcessorCount: processorCount,
                TotalPhysicalMemoryBytes: totalMemory,
                MachineName: machineName,
                UserName: userName,
                WorkingDirectory: workingDirectory);
        }
    }
}
