// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>

#include "cor.h"

#include "OpCode.h"
#include "Operand.h"

namespace LibProfiler
{
    class Instruction
    {
    public:
        Instruction(OpCode opCode, std::optional<Operand> operand, INT offset)
            : _opCode(opCode), _operand(operand), _offset(offset)
        {

        }

        [[nodiscard]] const INT GetSize() const;
        [[nodiscard]] const INT GetOffset() const;
        [[nodiscard]] const OpCode& GetOpCode() const;
        [[nodiscard]] const std::optional<Operand> GetOperand() const;

    private:
        INT _offset;
        OpCode _opCode;
        std::optional<Operand> _operand;
    };
}