// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <stdexcept>

#include "Instruction.h"
#include "MethodBodyHelpers.h"
#include "OpCodes.h"

std::tuple<UINT, UINT> LibProfiler::ReadHeaderInfo(const BYTE* data, INT& index)
{
    const auto header = data[index];
    const auto isFat = (header & 0x03) == 0x03;
    if (!isFat)
    {
        // Tiny header
        index++;
        return std::make_tuple(1, static_cast<UINT>((header & 0xFC) >> 2));
    }
    else
    {
        // Fat header
        const auto fatHeader = (IMAGE_COR_ILMETHOD_FAT*)data;
        constexpr UINT headerSize = sizeof(IMAGE_COR_ILMETHOD_FAT);
        const UINT codeSize = fatHeader->CodeSize;
        index += headerSize;
        return std::make_tuple(headerSize, codeSize);
    }
}

LibProfiler::Instruction LibProfiler::ReadInstruction(const BYTE* data, INT& index)
{
    const auto offset = index;
    const auto opCode = ReadOpCode(data, index);
    const auto operand = ReadOperand(opCode, data, index);
    auto instruction = Instruction(opCode, operand, offset);

    if (opCode.GetCode() == Code::Switch)
        index += (4 * operand.value().Arg32);

    return instruction;
}

LibProfiler::OpCode LibProfiler::ReadOpCode(const BYTE* data, INT& index)
{
    const auto op = data[index++];
    if (op == 0xFE)
    {
        auto& opcode = OpCodes::TwoByteOpCodes[data[index++]];
        return opcode.value();
    }
        
    auto& opcode = OpCodes::OneByteOpCodes[op];
    return opcode.value();
}

std::optional<LibProfiler::Operand> LibProfiler::ReadOperand(const OpCode &opCode, const BYTE* data, INT& index)
{
    switch (opCode.GetOperandType())
    {
        case OperandType::InlineI8:
            return ReadInlineI64(data, index);
        case OperandType::InlineNone:
        case OperandType::InlinePhi:
            return { };
        case OperandType::InlineR:
            return ReadInlineR(data, index);
        case OperandType::ShortInlineR:
            return ReadShortInlineR(data, index);
        case OperandType::InlineMethod:
        case OperandType::InlineBrTarget:
        case OperandType::InlineField:
        case OperandType::InlineI:
        case OperandType::InlineSig:
        case OperandType::InlineString:
        case OperandType::InlineSwitch:
        case OperandType::InlineTok:
        case OperandType::InlineType:
            return ReadInlineI32(data, index);
        case OperandType::InlineVar:
            return ReadInlineI16(data, index);
        case OperandType::ShortInlineBrTarget:
        case OperandType::ShortInlineI:
        case OperandType::ShortInlineVar:
            return ReadInlineI8(data, index);
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
