// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Core.Plugins;

public delegate void BoundMethodEnterExitHandler(
    IPlugin plugin,
    RecordedEventMetadata metadata,
    IRecordedEventArgs args);
