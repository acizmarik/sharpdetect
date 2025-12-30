// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>
#include <utility>

#include "cor.h"

#include "OpCode.h"
#include "Operand.h"

namespace LibProfiler
{
    class Instruction
    {
    public:
        Instruction(OpCode opCode, const std::optional<Operand> operand, const INT offset)
            : _offset(offset), _opCode(std::move(opCode)), _operand(operand)
        {

        }

        [[nodiscard]] INT GetSize() const;
        [[nodiscard]] INT GetOffset() const;
        [[nodiscard]] const OpCode& GetOpCode() const;
        [[nodiscard]] std::optional<Operand> GetOperand() const;

    private:
        INT _offset;
        OpCode _opCode;
        std::optional<Operand> _operand;
    };
}