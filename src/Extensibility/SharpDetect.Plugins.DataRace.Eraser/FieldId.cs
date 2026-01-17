// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using dnlib.DotNet;

namespace SharpDetect.Plugins.DataRace.Eraser;

[DebuggerDisplay("Process:{ProcessId}/Field:{FieldDef.FullName}")]
internal readonly record struct FieldId(uint ProcessId, FieldDef FieldDef);