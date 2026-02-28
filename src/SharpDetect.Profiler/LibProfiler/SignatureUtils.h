// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"

namespace LibProfiler
{
    unsigned SkipSigType(const BYTE* signature, unsigned length);

    bool SigTypeContainsGenericParam(const BYTE* signature, unsigned length);
}

