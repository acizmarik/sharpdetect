// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using System.Collections.Immutable;

namespace SharpDetect.Reporting.Model
{
    public class Summary
    {
        public readonly string Title;
        public readonly string Description;
        public readonly RuntimeInfo RuntimeInfo;
        public readonly RewritingInfo RewritingInfo;
        private readonly ImmutableArray<Report> _reports;
        private readonly Dictionary<string, string> _collectionProperties;
        private readonly Dictionary<string, string> _runtimeProperties;
        private readonly ImmutableArray<ModuleInfo> _modules;

        public Summary(
            string title,
            string description,
            RuntimeInfo runtimeInfo,
            RewritingInfo rewritingInfo,
            Dictionary<string, string> runtimeProps,
            Dictionary<string, string> collectionProps,
            IEnumerable<ModuleInfo> modules,
            IEnumerable<Report> reports)
        {
            Title = title;
            Description = description;
            RuntimeInfo = runtimeInfo;
            RewritingInfo = rewritingInfo;
            _runtimeProperties = runtimeProps;
            _collectionProperties = collectionProps;
            _modules = modules.ToImmutableArray();
            _reports = reports.ToImmutableArray();

        }

        public IReadOnlyDictionary<string, string> GetRuntimeProperties()
            => _runtimeProperties;

        public IReadOnlyDictionary<string, string> GetCollectionProperties()
            => _collectionProperties;

        public IEnumerable<ModuleInfo> GetAllModules()
            => _modules;

        public IEnumerable<Report> GetAllReports()
            => _reports;
    }
}
