// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common
{
    public enum AnalysisEventType
    {
        Unknown,
        ArrayElementRead,
        ArrayElementWrite,
        FieldRead,
        FieldWrite,
        ObjectCreation
    }
}
