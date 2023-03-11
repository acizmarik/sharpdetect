// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common
{
    public enum MethodInterpretation
    {
        Regular,

        FieldAccess,
        FieldInstanceAccess,

        ArrayIndexAccess,
        ArrayInstanceAccess,
        ArrayElementAccess,

        LockBlockingAcquire,
        LockTryAcquire,
        LockRelease,

        SignalBlockingWait,
        SignalTryWait,
        SignalPulseOne,
        SignalPulseAll
    }
}
