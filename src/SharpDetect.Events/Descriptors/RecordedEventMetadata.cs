// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Events;

public readonly record struct RecordedEventMetadata(
    uint Pid,
    ThreadId Tid);
