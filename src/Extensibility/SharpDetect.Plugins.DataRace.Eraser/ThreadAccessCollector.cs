// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class ThreadAccessCollector
{
    private readonly record struct ThreadAccessEntry(
        DataRaceInfo Race,
        AccessInfo Access,
        bool IsCurrent);

    private readonly Dictionary<ProcessThreadId, List<ThreadAccessEntry>> _accessesByThread = [];
    
    public void AddRace(DataRaceInfo race)
    {
        AddAccess(race.CurrentAccess.ProcessThreadId, race, race.CurrentAccess, isCurrent: true);
        if (race.LastAccess != null)
            AddAccess(race.LastAccess.ProcessThreadId, race, race.LastAccess, isCurrent: false);
    }

    public IEnumerable<ProcessThreadId> GetThreads() => _accessesByThread.Keys;
    
    public AccessInfo GetFirstAccess(ProcessThreadId threadId)
    {
        return _accessesByThread[threadId].First().Access;
    }
    
    public IEnumerable<(DataRaceInfo Race, AccessInfo Access, bool IsCurrent)> GetDistinctAccesses(ProcessThreadId threadId)
    {
        return _accessesByThread[threadId]
            .DistinctBy(a => (a.Access.Timestamp, a.Access.AccessType))
            .Select(e => (e.Race, e.Access, e.IsCurrent));
    }
    
    public IEnumerable<AccessInfo> GetDistinctMethods(ProcessThreadId threadId)
    {
        return _accessesByThread[threadId]
            .Select(a => a.Access)
            .DistinctBy(a => a.MethodToken.Value);
    }

    private void AddAccess(ProcessThreadId threadId, DataRaceInfo race, AccessInfo access, bool isCurrent)
    {
        if (!_accessesByThread.TryGetValue(threadId, out var list))
        {
            list = [];
            _accessesByThread[threadId] = list;
        }
        
        list.Add(new ThreadAccessEntry(race, access, isCurrent));
    }
}
