// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Descriptors;
using SharpDetect.Core.Reporting.Model;
using System.Collections.Immutable;

namespace SharpDetect.Core.Plugins;

public interface IPlugin
{
    string ReportCategory { get; }
    ImmutableArray<MethodDescriptor> MethodDescriptors { get; }
    COR_PRF_MONITOR ProfilerMonitoringOptions { get; }
    
    Summary CreateDiagnostics();

    RecordedEventActionVisitorBase EventsVisitor { get; }
}
