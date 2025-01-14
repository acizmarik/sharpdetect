// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Core.Plugins;

public interface IPluginHost
{
    RecordedEventState ProcessEvent(RecordedEvent recordedEvent);
}