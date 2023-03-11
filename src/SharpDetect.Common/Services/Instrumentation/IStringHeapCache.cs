// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;

namespace SharpDetect.Common.Services.Instrumentation
{
    public interface IStringHeapCache
    {
        MDToken GetStringOffset(ModuleDef module, string str);
    }
}
