// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using dnlib.DotNet;

namespace SharpDetect.Plugins.DataRace.Common;

[DebuggerDisplay("Process:{ProcessId}/Field:{FieldDef.FullName}")]
public readonly record struct FieldId(uint ProcessId, FieldDef FieldDef);

