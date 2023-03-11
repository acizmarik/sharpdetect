// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.LibraryDescriptors
{
    [Flags]
    public enum MethodRewritingFlags
    {
        None,
        InjectEntryExitHooks,
        CaptureArguments,
        CaptureReturnValue,
        InjectManagedWrapper
    }
}
