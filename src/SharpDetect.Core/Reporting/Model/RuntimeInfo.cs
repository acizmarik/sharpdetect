// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Reporting.Model;

public record RuntimeInfo(COR_PRF_RUNTIME_TYPE Type, Version Version);
