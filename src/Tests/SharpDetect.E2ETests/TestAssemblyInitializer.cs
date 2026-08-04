// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Configuration;
using System.Runtime.CompilerServices;

namespace SharpDetect.E2ETests;

internal static class TestAssemblyInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        EnvironmentUtils.Initialize();
    }
}
