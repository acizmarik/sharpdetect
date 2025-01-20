// dnlib: See LICENSE.dnlib.txt for more info

#pragma once

#include <array>

#include "../lib/optional/include/tl/optional.hpp"

#include "Code.h"
#include "OpCode.h"

namespace LibProfiler
{
    /// <summary>
    /// Contains all valid CIL opcodes
    /// </summary>
    struct OpCodes
    {
    public:
        /// <summary>
        /// All one-byte opcodes
        /// </summary>
        static std::array<tl::optional<LibProfiler::OpCode>, 256> OneByteOpCodes;

        /// <summary>
        /// All two-byte opcodes (first byte is <c>0xFE</c>)
        /// </summary>
        static std::array<tl::optional<LibProfiler::OpCode>, 256> TwoByteOpCodes;

        static void Register(LibProfiler::OpCode&& opCode)
        {
            auto code = opCode.GetCode();
            if (((USHORT)code >> 8) == 0)
                OpCodes::OneByteOpCodes[(BYTE)code] = std::move(opCode);
            else if (((USHORT)code >> 8) == 0xFE)
                OpCodes::TwoByteOpCodes[(BYTE)code] = std::move(opCode);
        }

        static void Initialize()
        {
            Register(OpCode("UNKNOWN1", Code::UNKNOWN1, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("UNKNOWN1", Code::UNKNOWN1, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("UNKNOWN2", Code::UNKNOWN2, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("nop", Code::Nop, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("break", Code::Break, OperandType::InlineNone, FlowControl::Break, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("ldarg::0", Code::Ldarg_0, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldarg::1", Code::Ldarg_1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldarg::2", Code::Ldarg_2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldarg::3", Code::Ldarg_3, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldloc::0", Code::Ldloc_0, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldloc::1", Code::Ldloc_1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldloc::2", Code::Ldloc_2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldloc::3", Code::Ldloc_3, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("stloc::0", Code::Stloc_0, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("stloc::1", Code::Stloc_1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("stloc::2", Code::Stloc_2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("stloc::3", Code::Stloc_3, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("ldarg::s", Code::Ldarg_S, OperandType::ShortInlineVar, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldarga::s", Code::Ldarga_S, OperandType::ShortInlineVar, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("starg::s", Code::Starg_S, OperandType::ShortInlineVar, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("ldloc::s", Code::Ldloc_S, OperandType::ShortInlineVar, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldloca::s", Code::Ldloca_S, OperandType::ShortInlineVar, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("stloc::s", Code::Stloc_S, OperandType::ShortInlineVar, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("ldnull", Code::Ldnull, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushref, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::m1", Code::Ldc_I4_M1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::0", Code::Ldc_I4_0, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::1", Code::Ldc_I4_1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::2", Code::Ldc_I4_2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::3", Code::Ldc_I4_3, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::4", Code::Ldc_I4_4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::5", Code::Ldc_I4_5, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::6", Code::Ldc_I4_6, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::7", Code::Ldc_I4_7, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::8", Code::Ldc_I4_8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4::s", Code::Ldc_I4_S, OperandType::ShortInlineI, FlowControl::Next, OpCodeType::Macro, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i4", Code::Ldc_I4, OperandType::InlineI, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldc::i8", Code::Ldc_I8, OperandType::InlineI8, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Pop0));
            Register(OpCode("ldc::r4", Code::Ldc_R4, OperandType::ShortInlineR, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr4, StackBehaviour::Pop0));
            Register(OpCode("ldc::r8", Code::Ldc_R8, OperandType::InlineR, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr8, StackBehaviour::Pop0));
            Register(OpCode("dup", Code::Dup, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1_push1, StackBehaviour::Pop1));
            Register(OpCode("pop", Code::Pop, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("jmp", Code::Jmp, OperandType::InlineMethod, FlowControl::Call, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("call", Code::Call, OperandType::InlineMethod, FlowControl::Call, OpCodeType::Primitive, StackBehaviour::Varpush, StackBehaviour::Varpop));
            Register(OpCode("calli", Code::Calli, OperandType::InlineSig, FlowControl::Call, OpCodeType::Primitive, StackBehaviour::Varpush, StackBehaviour::Varpop));
            Register(OpCode("ret", Code::Ret, OperandType::InlineNone, FlowControl::Return, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Varpop));
            Register(OpCode("br::s", Code::Br_S, OperandType::ShortInlineBrTarget, FlowControl::Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("brfalse::s", Code::Brfalse_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Popi));
            Register(OpCode("brtrue::s", Code::Brtrue_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Popi));
            Register(OpCode("beq::s", Code::Beq_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bge::s", Code::Bge_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bgt::s", Code::Bgt_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("ble::s", Code::Ble_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("blt::s", Code::Blt_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bne::un::s", Code::Bne_Un_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bge::un::s", Code::Bge_Un_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bgt::un::s", Code::Bgt_Un_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("ble::un::s", Code::Ble_Un_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("blt::un::s", Code::Blt_Un_S, OperandType::ShortInlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("br", Code::Br, OperandType::InlineBrTarget, FlowControl::Branch, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("brfalse", Code::Brfalse, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi));
            Register(OpCode("brtrue", Code::Brtrue, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi));
            Register(OpCode("beq", Code::Beq, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bge", Code::Bge, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bgt", Code::Bgt, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("ble", Code::Ble, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("blt", Code::Blt, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bne::un", Code::Bne_Un, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bge::un", Code::Bge_Un, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("bgt::un", Code::Bgt_Un, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("ble::un", Code::Ble_Un, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("blt::un", Code::Blt_Un, OperandType::InlineBrTarget, FlowControl::Cond_Branch, OpCodeType::Macro, StackBehaviour::Push0, StackBehaviour::Pop1_pop1));
            Register(OpCode("switch", Code::Switch, OperandType::InlineSwitch, FlowControl::Cond_Branch, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi));
            Register(OpCode("ldind::i1", Code::Ldind_I1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("ldind::u1", Code::Ldind_U1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("ldind::i2", Code::Ldind_I2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("ldind::u2", Code::Ldind_U2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("ldind::i4", Code::Ldind_I4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("ldind::u4", Code::Ldind_U4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("ldind::i8", Code::Ldind_I8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Popi));
            Register(OpCode("ldind::i", Code::Ldind_I, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("ldind::r4", Code::Ldind_R4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr4, StackBehaviour::Popi));
            Register(OpCode("ldind::r8", Code::Ldind_R8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr8, StackBehaviour::Popi));
            Register(OpCode("ldind::ref", Code::Ldind_Ref, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushref, StackBehaviour::Popi));
            Register(OpCode("stind::ref", Code::Stind_Ref, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi));
            Register(OpCode("stind::i1", Code::Stind_I1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi));
            Register(OpCode("stind::i2", Code::Stind_I2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi));
            Register(OpCode("stind::i4", Code::Stind_I4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi));
            Register(OpCode("stind::i8", Code::Stind_I8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi8));
            Register(OpCode("stind::r4", Code::Stind_R4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popr4));
            Register(OpCode("stind::r8", Code::Stind_R8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popr8));
            Register(OpCode("add", Code::Add, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("sub", Code::Sub, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("mul", Code::Mul, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("div", Code::Div, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("div::un", Code::Div_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("rem", Code::Rem, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("rem::un", Code::Rem_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("and", Code::And, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("or", Code::Or, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("xor", Code::Xor, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("shl", Code::Shl, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("shr", Code::Shr, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("shr::un", Code::Shr_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("neg", Code::Neg, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1));
            Register(OpCode("not", Code::Not, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1));
            Register(OpCode("conv::i1", Code::Conv_I1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::i2", Code::Conv_I2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::i4", Code::Conv_I4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::i8", Code::Conv_I8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Pop1));
            Register(OpCode("conv::r4", Code::Conv_R4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr4, StackBehaviour::Pop1));
            Register(OpCode("conv::r8", Code::Conv_R8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr8, StackBehaviour::Pop1));
            Register(OpCode("conv::u4", Code::Conv_U4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::u8", Code::Conv_U8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Pop1));
            Register(OpCode("callvirt", Code::Callvirt, OperandType::InlineMethod, FlowControl::Call, OpCodeType::Objmodel, StackBehaviour::Varpush, StackBehaviour::Varpop));
            Register(OpCode("cpobj", Code::Cpobj, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popi_popi));
            Register(OpCode("ldobj", Code::Ldobj, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push1, StackBehaviour::Popi));
            Register(OpCode("ldstr", Code::Ldstr, OperandType::InlineString, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushref, StackBehaviour::Pop0));
            Register(OpCode("newobj", Code::Newobj, OperandType::InlineMethod, FlowControl::Call, OpCodeType::Objmodel, StackBehaviour::Pushref, StackBehaviour::Varpop));
            Register(OpCode("castclass", Code::Castclass, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushref, StackBehaviour::Popref));
            Register(OpCode("isinst", Code::Isinst, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref));
            Register(OpCode("conv::r::un", Code::Conv_R_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr8, StackBehaviour::Pop1));
            Register(OpCode("unbox", Code::Unbox, OperandType::InlineType, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popref));
            Register(OpCode("throw", Code::Throw, OperandType::InlineNone, FlowControl::Throw, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref));
            Register(OpCode("ldfld", Code::Ldfld, OperandType::InlineField, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push1, StackBehaviour::Popref));
            Register(OpCode("ldflda", Code::Ldflda, OperandType::InlineField, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref));
            Register(OpCode("stfld", Code::Stfld, OperandType::InlineField, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_pop1));
            Register(OpCode("ldsfld", Code::Ldsfld, OperandType::InlineField, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldsflda", Code::Ldsflda, OperandType::InlineField, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("stsfld", Code::Stsfld, OperandType::InlineField, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("stobj", Code::Stobj, OperandType::InlineType, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_pop1));
            Register(OpCode("conv::ovf::i1::un", Code::Conv_Ovf_I1_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i2::un", Code::Conv_Ovf_I2_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i4::un", Code::Conv_Ovf_I4_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i8::un", Code::Conv_Ovf_I8_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u1::un", Code::Conv_Ovf_U1_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u2::un", Code::Conv_Ovf_U2_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u4::un", Code::Conv_Ovf_U4_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u8::un", Code::Conv_Ovf_U8_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i::un", Code::Conv_Ovf_I_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u::un", Code::Conv_Ovf_U_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("box", Code::Box, OperandType::InlineType, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushref, StackBehaviour::Pop1));
            Register(OpCode("newarr", Code::Newarr, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushref, StackBehaviour::Popi));
            Register(OpCode("ldlen", Code::Ldlen, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref));
            Register(OpCode("ldelema", Code::Ldelema, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::i1", Code::Ldelem_I1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::u1", Code::Ldelem_U1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::i2", Code::Ldelem_I2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::u2", Code::Ldelem_U2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::i4", Code::Ldelem_I4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::u4", Code::Ldelem_U4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::i8", Code::Ldelem_I8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi8, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::i", Code::Ldelem_I, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushi, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::r4", Code::Ldelem_R4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushr4, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::r8", Code::Ldelem_R8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushr8, StackBehaviour::Popref_popi));
            Register(OpCode("ldelem::ref", Code::Ldelem_Ref, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Pushref, StackBehaviour::Popref_popi));
            Register(OpCode("stelem::i", Code::Stelem_I, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popi));
            Register(OpCode("stelem::i1", Code::Stelem_I1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popi));
            Register(OpCode("stelem::i2", Code::Stelem_I2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popi));
            Register(OpCode("stelem::i4", Code::Stelem_I4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popi));
            Register(OpCode("stelem::i8", Code::Stelem_I8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popi8));
            Register(OpCode("stelem::r4", Code::Stelem_R4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popr4));
            Register(OpCode("stelem::r8", Code::Stelem_R8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popr8));
            Register(OpCode("stelem::ref", Code::Stelem_Ref, OperandType::InlineNone, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_popref));
            Register(OpCode("ldelem", Code::Ldelem, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push1, StackBehaviour::Popref_popi));
            Register(OpCode("stelem", Code::Stelem, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popref_popi_pop1));
            Register(OpCode("unbox::any", Code::Unbox_Any, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push1, StackBehaviour::Popref));
            Register(OpCode("conv::ovf::i1", Code::Conv_Ovf_I1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u1", Code::Conv_Ovf_U1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i2", Code::Conv_Ovf_I2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u2", Code::Conv_Ovf_U2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i4", Code::Conv_Ovf_I4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u4", Code::Conv_Ovf_U4, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i8", Code::Conv_Ovf_I8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u8", Code::Conv_Ovf_U8, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi8, StackBehaviour::Pop1));
            Register(OpCode("refanyval", Code::Refanyval, OperandType::InlineType, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("ckfinite", Code::Ckfinite, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushr8, StackBehaviour::Pop1));
            Register(OpCode("mkrefany", Code::Mkrefany, OperandType::InlineType, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Popi));
            Register(OpCode("ldtoken", Code::Ldtoken, OperandType::InlineTok, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("conv::u2", Code::Conv_U2, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::u1", Code::Conv_U1, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::i", Code::Conv_I, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::i", Code::Conv_Ovf_I, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("conv::ovf::u", Code::Conv_Ovf_U, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("add::ovf", Code::Add_Ovf, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("add::ovf::un", Code::Add_Ovf_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("mul::ovf", Code::Mul_Ovf, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("mul::ovf::un", Code::Mul_Ovf_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("sub::ovf", Code::Sub_Ovf, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("sub::ovf::un", Code::Sub_Ovf_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop1_pop1));
            Register(OpCode("endfinally", Code::Endfinally, OperandType::InlineNone, FlowControl::Return, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::PopAll));
            Register(OpCode("leave", Code::Leave, OperandType::InlineBrTarget, FlowControl::Branch, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::PopAll));
            Register(OpCode("leave::s", Code::Leave_S, OperandType::ShortInlineBrTarget, FlowControl::Branch, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::PopAll));
            Register(OpCode("stind::i", Code::Stind_I, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi));
            Register(OpCode("conv::u", Code::Conv_U, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("prefix7", Code::Prefix7, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("prefix6", Code::Prefix6, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("prefix5", Code::Prefix5, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("prefix4", Code::Prefix4, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("prefix3", Code::Prefix3, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("prefix2", Code::Prefix2, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("prefix1", Code::Prefix1, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("prefixref", Code::Prefixref, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Nternal, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("arglist", Code::Arglist, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ceq", Code::Ceq, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1_pop1));
            Register(OpCode("cgt", Code::Cgt, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1_pop1));
            Register(OpCode("cgt::un", Code::Cgt_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1_pop1));
            Register(OpCode("clt", Code::Clt, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1_pop1));
            Register(OpCode("clt::un", Code::Clt_Un, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1_pop1));
            Register(OpCode("ldftn", Code::Ldftn, OperandType::InlineMethod, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("ldvirtftn", Code::Ldvirtftn, OperandType::InlineMethod, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popref));
            Register(OpCode("ldarg", Code::Ldarg, OperandType::InlineVar, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldarga", Code::Ldarga, OperandType::InlineVar, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("starg", Code::Starg, OperandType::InlineVar, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("ldloc", Code::Ldloc, OperandType::InlineVar, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push1, StackBehaviour::Pop0));
            Register(OpCode("ldloca", Code::Ldloca, OperandType::InlineVar, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("stloc", Code::Stloc, OperandType::InlineVar, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Pop1));
            Register(OpCode("localloc", Code::Localloc, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Popi));
            Register(OpCode("endfilter", Code::Endfilter, OperandType::InlineNone, FlowControl::Return, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi));
            Register(OpCode("unaligned::", Code::Unaligned, OperandType::ShortInlineI, FlowControl::Meta, OpCodeType::Prefix, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("volatile::", Code::Volatile, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Prefix, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("tail::", Code::Tailcall, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Prefix, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("initobj", Code::Initobj, OperandType::InlineType, FlowControl::Next, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Popi));
            Register(OpCode("constrained::", Code::Constrained, OperandType::InlineType, FlowControl::Meta, OpCodeType::Prefix, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("cpblk", Code::Cpblk, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi_popi));
            Register(OpCode("initblk", Code::Initblk, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Push0, StackBehaviour::Popi_popi_popi));
            Register(OpCode("no::", Code::No, OperandType::ShortInlineI, FlowControl::Meta, OpCodeType::Prefix, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("rethrow", Code::Rethrow, OperandType::InlineNone, FlowControl::Throw, OpCodeType::Objmodel, StackBehaviour::Push0, StackBehaviour::Pop0));
            Register(OpCode("sizeof", Code::Sizeof, OperandType::InlineType, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop0));
            Register(OpCode("refanytype", Code::Refanytype, OperandType::InlineNone, FlowControl::Next, OpCodeType::Primitive, StackBehaviour::Pushi, StackBehaviour::Pop1));
            Register(OpCode("readonly::", Code::Readonly, OperandType::InlineNone, FlowControl::Meta, OpCodeType::Prefix, StackBehaviour::Push0, StackBehaviour::Pop0));
        }
    };
}