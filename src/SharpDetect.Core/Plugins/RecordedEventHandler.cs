// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Core.Plugins;

public delegate void RecordedEventHandler(
    RecordedEventMetadata metadata,
    IRecordedEventArgs args);
