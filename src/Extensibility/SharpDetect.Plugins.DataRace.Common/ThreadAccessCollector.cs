// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public enum RaceRole
{
    Triggering,
    Conflicting
}

public sealed class ThreadAccessCollector
{
    private readonly record struct ThreadAccessEntry(
        DataRaceInfo Race,
        AccessInfo Access,
        RaceRole Role);

    private readonly Dictionary<ProcessThreadId, List<ThreadAccessEntry>> _accessesByThread = [];

    public void AddRace(DataRaceInfo race)
    {
        AddAccess(race.CurrentAccess.ProcessThreadId, race, race.CurrentAccess, RaceRole.Triggering);
        if (race.LastAccess != null)
            AddAccess(race.LastAccess.ProcessThreadId, race, race.LastAccess, RaceRole.Conflicting);
    }

    public IEnumerable<ProcessThreadId> GetThreads() => _accessesByThread.Keys;

    public AccessInfo GetFirstAccess(ProcessThreadId threadId)
    {
        return _accessesByThread[threadId].First().Access;
    }

    public IEnumerable<(DataRaceInfo Race, AccessInfo Access, RaceRole Role)> GetDistinctAccesses(ProcessThreadId threadId)
    {
        return _accessesByThread[threadId]
            .DistinctBy(a => (a.Access.Timestamp, a.Access.AccessType))
            .Select(e => (e.Race, e.Access, e.Role));
    }

    public IEnumerable<AccessInfo> GetDistinctMethods(ProcessThreadId threadId)
    {
        return _accessesByThread[threadId]
            .Select(a => a.Access)
            .DistinctBy(a => a.MethodToken.Value);
    }

    private void AddAccess(ProcessThreadId threadId, DataRaceInfo race, AccessInfo access, RaceRole role)
    {
        if (!_accessesByThread.TryGetValue(threadId, out var list))
        {
            list = [];
            _accessesByThread[threadId] = list;
        }

        list.Add(new ThreadAccessEntry(race, access, role));
    }
}

