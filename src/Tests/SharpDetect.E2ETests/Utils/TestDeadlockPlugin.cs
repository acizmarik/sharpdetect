// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Configuration;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.Deadlock;

namespace SharpDetect.E2ETests.Utils;

public sealed class TestDeadlockPlugin : DeadlockPlugin
{
    public event Action<(RecordedEventMetadata Metadata, StackTraceSnapshotsRecordedEvent Args)>? StackTraceSnapshotsCreated;
    
    public TestDeadlockPlugin(ICallstackResolver callstackResolver, TimeProvider timeProvider, PathsConfiguration pathsConfiguration, IServiceProvider serviceProvider)
        : base(callstackResolver, timeProvider, pathsConfiguration, serviceProvider)
    {
        
    }

    protected override void Visit(RecordedEventMetadata metadata, StackTraceSnapshotsRecordedEvent args)
    {
        base.Visit(metadata, args);
        StackTraceSnapshotsCreated?.Invoke((metadata, args));
    }
}