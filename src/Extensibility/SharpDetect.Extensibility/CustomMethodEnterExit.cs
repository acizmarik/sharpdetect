// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events;
using SharpDetect.Extensibility.Abstractions;

namespace SharpDetect.Extensibility
{
    public delegate void CustomMethodEnterExit(
        IPlugin plugin, 
        RecordedEventMetadata metadata, 
        IRecordedEventArgs args);
}
