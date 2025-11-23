// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <stdexcept>

#include "../lib/loguru/loguru.hpp"

#include "Instruction.h"
#include "MethodBodyHelpers.h"
#include "OpCodes.h"

std::tuple<UINT, UINT> LibProfiler::ReadHeaderInfo(const BYTE* data, INT& index)
{
    auto header = data[index];
    auto isFat = (header & 0x03) == 0x03;
    if (!isFat)
    {
        // Tiny header
        index++;
        return std::make_tuple(1, (UINT)((header & 0xFC) >> 2));
    }
    else
    {
        // Fat header
        auto header = (IMAGE_COR_ILMETHOD_FAT*)data;
        const UINT headerSize = sizeof(IMAGE_COR_ILMETHOD_FAT);
        const UINT codeSize = header->CodeSize;
        index += headerSize;
        return std::make_tuple(headerSize, codeSize);
    }
}

LibProfiler::Instruction LibProfiler::ReadInstruction(const BYTE* data, INT& index)
{
    auto offset = index;
    auto opCode = ReadOpCode(data, index);
    auto operand = ReadOperand(opCode, data, index);
    auto instruction = Instruction(opCode, operand, offset);

    if (opCode.GetCode() == Code::Switch)
        index += (4 * operand.value().Arg32);

    return instruction;
}

LibProfiler::OpCode LibProfiler::ReadOpCode(const BYTE* data, INT& index)
{
    auto op = data[index++];
    if (op == 0xFE)
    {
        auto& opcode = OpCodes::TwoByteOpCodes[data[index++]];
        if (!opcode.has_value())
        {
            LOG_F(ERROR, "UPS");
        }

        return opcode.value();
    }
        
    auto& opcode = OpCodes::OneByteOpCodes[op];
    if (!opcode.has_value())
    {
        LOG_F(ERROR, "UPS");
    }

    return opcode.value();
}

std::optional<LibProfiler::Operand> LibProfiler::ReadOperand(OpCode opCode, const BYTE* data, INT& index)
{
    switch (opCode.GetOperandType())
    {
        case OperandType::InlineBrTarget: return ReadInlineI32(data, index);
        case OperandType::InlineField: return ReadInlineI32(data, index);
        case OperandType::InlineI: return ReadInlineI32(data, index);
        case OperandType::InlineI8: return ReadInlineI64(data, index);
        case OperandType::InlineMethod: return ReadInlineI32(data, index);
        case OperandType::InlineNone: return { };
        case OperandType::InlinePhi: return { };
        case OperandType::InlineR: return ReadInlineR(data, index);
        case OperandType::InlineSig: return ReadInlineI32(data, index);
        case OperandType::InlineString: return ReadInlineI32(data, index);
        case OperandType::InlineSwitch: return ReadInlineI32(data, index);
        case OperandType::InlineTok: return ReadInlineI32(data, index);
        case OperandType::InlineType: return ReadInlineI32(data, index);
        case OperandType::InlineVar: return ReadInlineI16(data, index);
        case OperandType::ShortInlineBrTarget: return ReadInlineI8(data, index);
        case OperandType::ShortInlineI: return ReadInlineI8(data, index);
        case OperandType::ShortInlineR: return ReadShortInlineR(data, index);
        case OperandType::ShortInlineVar: return ReadInlineI8(data, index);
        default: throw std::runtime_error("Invalid OpCode.OperandType");
    }
}

LibProfiler::Operand LibProfiler::ReadInlineR(const BYTE* data, INT& index)
{
    Operand result{ };
    result.Real = Read<DOUBLE>(data, index);
    return result;
}

LibProfiler::Operand LibProfiler::ReadShortInlineR(const BYTE* data, INT& index)
{
    Operand result{ };
    result.Single = Read<FLOAT>(data, index);
    return result;
}

LibProfiler::Operand LibProfiler::ReadInlineI8(const BYTE* data, INT& index)
{
    Operand result{ };
    result.Arg8 = Read<INT8>(data, index);
    return result;
}

LibProfiler::Operand LibProfiler::ReadInlineI16(const BYTE* data, INT& index)
{
    Operand result{ };
    result.Arg16 = Read<INT16>(data, index);
    return result;
}

LibProfiler::Operand LibProfiler::ReadInlineI32(const BYTE* data, INT& index)
{
    Operand result{ };
    result.Arg32 = Read<INT32>(data, index);
    return result;
}

LibProfiler::Operand LibProfiler::ReadInlineI64(const BYTE* data, INT& index)
{
    Operand result{ };
    result.Arg64 = Read<INT64>(data, index);
    return result;
}
