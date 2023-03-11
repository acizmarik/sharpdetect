// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;

namespace SharpDetect.Common
{
    public record struct TypeInfo(UIntPtr ModuleId, MDToken TypeToken);
}
