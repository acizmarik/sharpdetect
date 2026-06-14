// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"
#include <vector>

namespace LibProfiler
{
    unsigned SkipSigType(const BYTE* signature, unsigned length);

    bool SigTypeContainsGenericParam(const BYTE* signature, unsigned length);

    bool ResolveSigType(
        const BYTE* typeSignature,
        unsigned typeSignatureLength,
        const std::vector<std::pair<const BYTE*, unsigned>>& typeArgs,
        std::vector<BYTE>& resolved);

    bool ParseTypeSpecGenericArgs(
        const BYTE* typeSpecSignature,
        unsigned typeSpecSigLength,
        std::vector<std::pair<const BYTE*, unsigned>>& typeArgs);
}

