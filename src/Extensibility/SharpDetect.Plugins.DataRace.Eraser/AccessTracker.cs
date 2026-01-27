// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class AccessTracker(TimeProvider timeProvider, Func<ProcessThreadId, string?> threadNameResolver)
{
    private readonly Dictionary<FieldId, AccessInfo> _lastAccessInfo = [];
    
    public AccessInfo? GetLastAccess(FieldId fieldId)
    {
        return _lastAccessInfo.GetValueOrDefault(fieldId);
    }

    public void RecordAccess(FieldId fieldId, AccessInfo accessInfo)
    {
        _lastAccessInfo[fieldId] = accessInfo;
    }
    
    public AccessInfo CreateAccessInfo(
        ProcessThreadId threadId,
        Core.Events.Profiler.ModuleId moduleId,
        Core.Events.Profiler.MdMethodDef methodToken,
        AccessType accessType)
    {
        return new AccessInfo(
            threadId,
            threadNameResolver(threadId),
            moduleId,
            methodToken,
            accessType,
            timeProvider.GetUtcNow().DateTime);
    }
}
