// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.Deadlock;

internal sealed class WaitForGraph
{
    public readonly uint ProcessId;
    private readonly Dictionary<ProcessThreadId, HashSet<ProcessThreadId>> _edges = [];
    private readonly Dictionary<ProcessThreadId, WaitInfo> _waitInfo = [];

    public WaitForGraph(uint processId)
    {
        ProcessId = processId;
    }
    
    public WaitInfo? GetWaitInfo(ProcessThreadId processThreadId)
    {
        return _waitInfo.GetValueOrDefault(processThreadId);
    }
    
    public void SetThreadWaiting(ProcessThreadId processThreadId, ProcessThreadId waitingFor, WaitInfo waitInfo)
    {
        EnsureThreadExists(processThreadId);
        EnsureThreadExists(waitingFor);
        
        _edges[processThreadId].Add(waitingFor);
        _waitInfo[processThreadId] = waitInfo;
    }
    
    public void ClearThreadWaiting(ProcessThreadId processThreadId)
    {
        EnsureThreadExists(processThreadId);
        _edges[processThreadId].Clear();
        _waitInfo.Remove(processThreadId);
    }
    
    public IEnumerable<ImmutableArray<ProcessThreadId>> DetectDeadlocks()
    {
        var state = new TarjanState();
        
        foreach (var node in _edges.Keys)
        {
            if (state.Indices.ContainsKey(node))
                continue;
            
            foreach (var scc in StrongConnect(node, state))
            {
                // Only consider SCCs with 2 or more nodes as deadlocks
                if (scc.Length > 1)
                    yield return scc;
            }
        }
    }

    private IEnumerable<ImmutableArray<ProcessThreadId>> StrongConnect(ProcessThreadId node, TarjanState state)
    {
        state.Indices[node] = state.Index;
        state.LowLinks[node] = state.Index;
        state.Index++;
        
        state.Stack.Push(node);
        state.OnStack.Add(node);

        if (_edges.TryGetValue(node, out var successors))
        {
            foreach (var successor in successors)
            {
                if (!state.Indices.TryGetValue(successor, out var index))
                {
                    foreach (var scc in StrongConnect(successor, state))
                        yield return scc;
                    
                    state.LowLinks[node] = Math.Min(state.LowLinks[node], state.LowLinks[successor]);
                }
                else if (state.OnStack.Contains(successor))
                    state.LowLinks[node] = Math.Min(state.LowLinks[node], index);
            }
        }
        
        if (state.LowLinks[node] != state.Indices[node])
            yield break;
        
        var sccBuilder = ImmutableArray.CreateBuilder<ProcessThreadId>();
        ProcessThreadId current;
        do
        {
            current = state.Stack.Pop();
            state.OnStack.Remove(current);
            sccBuilder.Add(current);
        } while (current != node);

        yield return sccBuilder.ToImmutable();
    }
    
    private void EnsureThreadExists(ProcessThreadId processThreadId)
    {
        if (!_edges.ContainsKey(processThreadId))
        {
            _edges[processThreadId] = [];
        }
    }
}

