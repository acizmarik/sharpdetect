// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "Instruction.h"

INT LibProfiler::Instruction::GetSize() const
{
    auto& opCode = _opCode;
    switch (opCode.GetOperandType())
    {
        case LibProfiler::OperandType::InlineBrTarget:
        case LibProfiler::OperandType::InlineField:
        case LibProfiler::OperandType::InlineI:
        case LibProfiler::OperandType::InlineMethod:
        case LibProfiler::OperandType::InlineSig:
        case LibProfiler::OperandType::InlineString:
        case LibProfiler::OperandType::InlineTok:
        case LibProfiler::OperandType::InlineType:
        case LibProfiler::OperandType::ShortInlineR:
            return opCode.GetSize() + 4;

        case LibProfiler::OperandType::InlineI8:
        case LibProfiler::OperandType::InlineR:
            return opCode.GetSize() + 8;

        case LibProfiler::OperandType::InlineNone:
        case LibProfiler::OperandType::InlinePhi:
        default:
            return opCode.GetSize();

        case LibProfiler::OperandType::InlineSwitch:
            return opCode.GetSize() + 4 + _operand.value().Arg32 * 4;

        case LibProfiler::OperandType::InlineVar:
            return opCode.GetSize() + 2;

        case LibProfiler::OperandType::ShortInlineBrTarget:
        case LibProfiler::OperandType::ShortInlineI:
        case LibProfiler::OperandType::ShortInlineVar:
            return opCode.GetSize() + 1;
    }
}

INT LibProfiler::Instruction::GetOffset() const
{
    return _offset;
}

const LibProfiler::OpCode& LibProfiler::Instruction::GetOpCode() const
{
    return _opCode;
}

std::optional<LibProfiler::Operand> LibProfiler::Instruction::GetOperand() const
{
    return _operand;
}
