// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using dnlib.DotNet;

namespace SharpDetect.Plugins.DataRace.Common;

// Keyed by the definition - every token form of a field shares one identity
// Instances are told apart by their object
// Statics of generic types are told apart by the runtime handle of their declaring type's instantiation
[DebuggerDisplay("Process:{ProcessId}/Field:{FieldDef.FullName}/DeclaringType:{DeclaringTypeHandle}")]
public readonly record struct FieldId(uint ProcessId, FieldDef FieldDef, nuint DeclaringTypeHandle)
{
    public static FieldId Create(uint processId, FieldDef fieldDef, nuint declaringTypeHandle)
        => new(processId, fieldDef, fieldDef.IsStatic && fieldDef.DeclaringType.HasGenericParameters ? declaringTypeHandle : 0);
}

