// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class LockSetTable
{
    private readonly List<ImmutableArray<ProcessTrackedObjectId>> _lockSets = [];
    private readonly Dictionary<int, List<LockSetIndex>> _hashToIndices = [];
    private readonly Dictionary<(LockSetIndex, LockSetIndex), LockSetIndex> _intersectionCache = [];
    private readonly Dictionary<(LockSetIndex, ProcessTrackedObjectId), LockSetIndex> _addCache = [];
    private readonly Dictionary<(LockSetIndex, ProcessTrackedObjectId), LockSetIndex> _removeCache = [];

    public LockSetTable()
    {
        var emptySet = ImmutableArray<ProcessTrackedObjectId>.Empty;
        _lockSets.Add(emptySet);
        _hashToIndices[ComputeHash(emptySet.AsSpan())] = [LockSetIndex.Empty];
    }
    
    public LockSetIndex Intersect(LockSetIndex first, LockSetIndex second)
    {
        if (first.IsEmpty || second.IsEmpty)
            return LockSetIndex.Empty;
        
        if (first == second)
            return first;
        
        var cacheKey = first.Value <= second.Value ? (first, second) : (second, first);
        if (_intersectionCache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;
        
        var setA = _lockSets[first.Value];
        var setB = _lockSets[second.Value];
        
        var intersection = ComputeSortedIntersection(setA.AsSpan(), setB.AsSpan());
        var result = GetOrCreateInternal(intersection);
        
        _intersectionCache[cacheKey] = result;
        return result;
    }
    
    public LockSetIndex Add(LockSetIndex index, ProcessTrackedObjectId lockId)
    {
        var cacheKey = (index, lockId);
        if (_addCache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;
        
        var existing = _lockSets[index.Value];
        var insertionPoint = BinarySearch(existing.AsSpan(), lockId);
        
        if (insertionPoint >= 0)
        {
            _addCache[cacheKey] = index;
            return index;
        }
        
        var actualInsertionPoint = ~insertionPoint;
        var newSet = CreateWithInsertion(existing, lockId, actualInsertionPoint);
        var result = GetOrCreateInternal(newSet);
        
        _addCache[cacheKey] = result;
        return result;
    }

    public LockSetIndex Remove(LockSetIndex index, ProcessTrackedObjectId lockId)
    {
        if (index.IsEmpty)
            return index;
        
        var cacheKey = (index, lockId);
        if (_removeCache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;
        
        var existing = _lockSets[index.Value];
        var removeIndex = BinarySearch(existing.AsSpan(), lockId);
        
        if (removeIndex < 0)
        {
            _removeCache[cacheKey] = index;
            return index;
        }
        
        var newSet = CreateWithRemoval(existing, removeIndex);
        var result = GetOrCreateInternal(newSet);
        
        _removeCache[cacheKey] = result;
        return result;
    }
    
    public int Count => _lockSets.Count;

    private LockSetIndex GetOrCreateInternal(ImmutableArray<ProcessTrackedObjectId> sortedLocks)
    {
        var hash = ComputeHash(sortedLocks.AsSpan());
        
        if (_hashToIndices.TryGetValue(hash, out var existingIndices))
        {
            foreach (var existingIndex in existingIndices)
            {
                if (AreSetsEqual(_lockSets[existingIndex.Value].AsSpan(), sortedLocks.AsSpan()))
                    return existingIndex;
            }
        }
        else
        {
            existingIndices = [];
            _hashToIndices[hash] = existingIndices;
        }

        var newIndex = new LockSetIndex(_lockSets.Count);
        _lockSets.Add(sortedLocks);
        existingIndices.Add(newIndex);
        return newIndex;
    }

    private static int BinarySearch(ReadOnlySpan<ProcessTrackedObjectId> sortedSet, ProcessTrackedObjectId lockId)
    {
        var low = 0;
        var high = sortedSet.Length - 1;
        var targetValue = lockId.ObjectId.Value;
        
        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var midValue = sortedSet[mid].ObjectId.Value;
            
            if (midValue == targetValue)
                return mid;
            
            if (midValue < targetValue)
                low = mid + 1;
            else
                high = mid - 1;
        }
        
        return ~low;
    }

    private static ImmutableArray<ProcessTrackedObjectId> CreateWithInsertion(
        ImmutableArray<ProcessTrackedObjectId> existing,
        ProcessTrackedObjectId lockId,
        int insertionPoint)
    {
        var builder = ImmutableArray.CreateBuilder<ProcessTrackedObjectId>(existing.Length + 1);

        if (insertionPoint > 0)
            builder.AddRange(existing.AsSpan()[..insertionPoint]);
        
        builder.Add(lockId);
        
        if (insertionPoint < existing.Length)
            builder.AddRange(existing.AsSpan()[insertionPoint..]);
        
        return builder.MoveToImmutable();
    }

    private static ImmutableArray<ProcessTrackedObjectId> CreateWithRemoval(
        ImmutableArray<ProcessTrackedObjectId> existing,
        int removeIndex)
    {
        if (existing.Length == 1)
            return ImmutableArray<ProcessTrackedObjectId>.Empty;
        
        var builder = ImmutableArray.CreateBuilder<ProcessTrackedObjectId>(existing.Length - 1);
        
        if (removeIndex > 0)
            builder.AddRange(existing.AsSpan()[..removeIndex]);

        if (removeIndex < existing.Length - 1)
            builder.AddRange(existing.AsSpan()[(removeIndex + 1)..]);
        
        return builder.MoveToImmutable();
    }

    private static ImmutableArray<ProcessTrackedObjectId> ComputeSortedIntersection(
        ReadOnlySpan<ProcessTrackedObjectId> setA,
        ReadOnlySpan<ProcessTrackedObjectId> setB)
    {
        var builder = ImmutableArray.CreateBuilder<ProcessTrackedObjectId>();
        var indexSetA = 0;
        var indexSetB = 0;
        
        while (indexSetA < setA.Length && indexSetB < setB.Length)
        {
            var idA = setA[indexSetA].ObjectId.Value;
            var idB = setB[indexSetB].ObjectId.Value;
            
            if (idA == idB)
            {
                builder.Add(setA[indexSetA]);
                indexSetA++;
                indexSetB++;
            }
            else if (idA < idB)
            {
                indexSetA++;
            }
            else
            {
                indexSetB++;
            }
        }
        
        return builder.ToImmutable();
    }

    private static int ComputeHash(ReadOnlySpan<ProcessTrackedObjectId> locks)
    {
        var hash = new HashCode();
        foreach (var lockId in locks)
            hash.Add(lockId.ObjectId.Value);
        
        return hash.ToHashCode();
    }

    private static bool AreSetsEqual(
        ReadOnlySpan<ProcessTrackedObjectId> first,
        ReadOnlySpan<ProcessTrackedObjectId> second)
    {
        return first.SequenceEqual(second);
    }
}
