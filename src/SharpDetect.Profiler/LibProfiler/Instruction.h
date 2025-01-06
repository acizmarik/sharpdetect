// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>

#include "cor.h"

#include "OpCode.h"
#include "Operand.h"
#include "OperandType.h"

namespace LibProfiler
{
    class Instruction
    {
    public:
        Instruction(OpCode opCode, std::optional<Operand> operand, INT offset)
            : _opCode(opCode), _operand(operand), _offset(offset)
        {

        }

        const INT GetSize() const;
        const INT GetOffset() const;
        const OpCode& GetOpCode() const;
        const std::optional<Operand> GetOperand() const;

    private:
        INT _offset;
        OpCode _opCode;
        std::optional<Operand> _operand;
    };
}