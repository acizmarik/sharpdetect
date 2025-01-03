// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Reporting.Model
{
    public record RuntimeInfo(COR_PRF_RUNTIME_TYPE Type, Version Version);
}
