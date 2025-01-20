// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"
#include "../lib/optional/include/tl/optional.hpp"

#include "OpCode.h"
#include "Operand.h"
#include "OperandType.h"

namespace LibProfiler
{
    class Instruction
    {
    public:
        Instruction(OpCode opCode, tl::optional<Operand> operand, INT offset)
            : _opCode(opCode), _operand(operand), _offset(offset)
        {

        }

        const INT GetSize() const;
        const INT GetOffset() const;
        const OpCode& GetOpCode() const;
        const tl::optional<Operand> GetOperand() const;

    private:
        INT _offset;
        OpCode _opCode;
        tl::optional<Operand> _operand;
    };
}