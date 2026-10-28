// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using dnlib.DotNet;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Plugins.DataRace.Common;

[DebuggerDisplay("Process:{ProcessId}/Module:{ModuleId}/Token:{FieldToken}/Field:{FieldDef.FullName}")]
public readonly record struct FieldId(uint ProcessId, ModuleId ModuleId, MdToken FieldToken, FieldDef FieldDef)
{
    public static FieldId Create(uint processId, ModuleId moduleId, MdToken fieldToken, FieldDef fieldDef)
        => fieldDef.IsStatic
            ? new FieldId(processId, moduleId, fieldToken, fieldDef)
            : new FieldId(processId, default, default, fieldDef);
}

