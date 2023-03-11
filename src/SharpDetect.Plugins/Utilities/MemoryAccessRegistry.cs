// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using ConcurrentCollections;
using dnlib.DotNet;
using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Runtime;
using System.Collections.Concurrent;

namespace SharpDetect.Plugins.Utilities
{
    public class MemoryAccessRegistry
    {
        private readonly ConcurrentDictionary<MDToken, ConcurrentHashSet<ReportDataEntry>> fieldAccesses;
        private readonly ConcurrentDictionary<(IShadowObject, int), ConcurrentHashSet<ReportDataEntry>> arrayElementAccesses;

        public MemoryAccessRegistry()
        {
            fieldAccesses = new();
            arrayElementAccesses = new();
        }

        public void RegisterAccess(MDToken token, ReportDataEntry access)
        {
            var bag = fieldAccesses.GetOrAdd(token, _ => new());
            bag.Add(access);
        }

        public void RegisterAccess(IShadowObject instance, int index, ReportDataEntry access)
        {
            var bag = arrayElementAccesses.GetOrAdd((instance, index), _ => new());
            bag.Add(access);
        }

        public IEnumerable<ReportDataEntry> GetAllAccesses(MDToken token)
        {
            return fieldAccesses[token];
        }

        public IEnumerable<ReportDataEntry> GetAllAccesses(IShadowObject instance, int index)
        {
            return arrayElementAccesses[(instance, index)];
        }
    }
}
