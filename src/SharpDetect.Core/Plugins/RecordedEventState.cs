// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.Core.Plugins;

public enum RecordedEventState
{
    Unavailable = 0,
    Executed = 1,
    Defered = 2,
    Discarded = 3,
    Failed = 4
}
