// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.DataRace.Common;

public readonly record struct FieldDefOrRef(ModuleId ModuleId, MdToken Token);

