// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;
using SharpDetect.Extensibility.Descriptors;
using SharpDetect.Reporting.Model;
using System.Collections.Immutable;

namespace SharpDetect.Extensibility;

public interface IPlugin
{
    string ReportCategory { get; }
    ImmutableArray<MethodDescriptor> MethodDescriptors { get; }
    COR_PRF_MONITOR ProfilerMonitoringOptions { get; }
    
    Summary CreateDiagnostics();

    RecordedEventActionVisitorBase EventsVisitor { get; }
}
