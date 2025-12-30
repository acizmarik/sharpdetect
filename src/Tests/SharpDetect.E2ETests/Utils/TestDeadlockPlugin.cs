// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Configuration;
using SharpDetect.Core.Events;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Serialization;
using SharpDetect.Plugins.Deadlock;

namespace SharpDetect.E2ETests.Utils;

public sealed class TestDeadlockPlugin : DeadlockPlugin
{
    public event Action<(RecordedEventMetadata Metadata, StackTraceSnapshotsRecordedEvent Args)>? StackTraceSnapshotsCreated;
    
    public TestDeadlockPlugin(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IRecordedEventsDeliveryContext eventsDeliveryContext,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        ICallstackResolver callstackResolver,
        PathsConfiguration pathsConfiguration,
        TimeProvider timeProvider,
        ILogger<DeadlockPlugin> logger)
        : base(
            moduleBindContext,
            metadataContext,
            argumentsParser,
            eventsDeliveryContext,
            profilerCommandSenderProvider,
            callstackResolver,
            pathsConfiguration,
            timeProvider,
            logger)
    {
        
    }

    protected override void Visit(RecordedEventMetadata metadata, StackTraceSnapshotsRecordedEvent args)
    {
        base.Visit(metadata, args);
        StackTraceSnapshotsCreated?.Invoke((metadata, args));
    }
}