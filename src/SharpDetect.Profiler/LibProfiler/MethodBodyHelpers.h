// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstring>
#include <optional>
#include <tuple>

#include "cor.h"

#include "Instruction.h"
#include "OpCode.h"

namespace LibProfiler
{
    std::tuple<UINT, UINT> ReadHeaderInfo(const BYTE* data, INT& index);

    Instruction ReadInstruction(const BYTE* data, INT& index);

    OpCode ReadOpCode(const BYTE* data, INT& index);

    std::optional<Operand> ReadOperand(const OpCode &opCode, const BYTE* data, INT& index);

    template <class TInput>
    TInput Read(const BYTE* data, INT& index)
    {
        const auto readFrom = index;
        index += sizeof(TInput);

        TInput result { };
        std::memcpy(&result, &data[readFrom], sizeof(TInput));
        return result;
    }

    Operand ReadInlineR(const BYTE* data, INT& index);
    Operand ReadShortInlineR(const BYTE* data, INT& index);
    Operand ReadInlineI8(const BYTE* data, INT& index);
    Operand ReadInlineI16(const BYTE* data, INT& index);
    Operand ReadInlineI32(const BYTE* data, INT& index);
    Operand ReadInlineI64(const BYTE* data, INT& index);
}