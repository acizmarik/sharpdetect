// dnlib: See LICENSE.dnlib.txt for more info

#pragma once

#include <string>
#include <utility>

#include "cor.h"

#include "Code.h"
#include "FlowControl.h"
#include "OpCodeType.h"
#include "OperandType.h"
#include "StackBehaviour.h"

namespace LibProfiler
{
    class OpCode
    {
    public:
        OpCode(
            std::string name,
            const Code code,
            const OperandType operandType,
            const FlowControl flowControl,
            const OpCodeType opCodeType,
            const StackBehaviour stackBehaviourPush,
            const StackBehaviour stackBehaviourPop) :
            _name(std::move(name)),
            _code(code),
            _operandType(operandType),
            _flowControl(flowControl),
            _opCodeType(opCodeType),
            _stackBehaviourPush(stackBehaviourPush),
            _stackBehaviourPop(stackBehaviourPop)
        {

        }

        [[nodiscard]] constexpr SHORT GetValue() const { return static_cast<SHORT>(_code); }
        [[nodiscard]] constexpr INT GetSize() const { return _code < static_cast<Code>(0x100) || _code == Code::UNKNOWN1 ? 1 : 2; }
        [[nodiscard]] const std::string& GetName() const { return _name; }
        [[nodiscard]] constexpr Code GetCode() const { return _code; }
        [[nodiscard]] constexpr OperandType GetOperandType() const { return _operandType; }
        [[nodiscard]] constexpr FlowControl GetFlowControl() const { return _flowControl; }
        [[nodiscard]] constexpr OpCodeType GetOpCodeType() const { return _opCodeType; }
        [[nodiscard]] constexpr StackBehaviour GetStackBehaviourPush() const { return _stackBehaviourPush; }
        [[nodiscard]] constexpr StackBehaviour GetStackBehaviourPop() const { return _stackBehaviourPop; }

    private:
        std::string _name;
        Code _code;
        OperandType _operandType;
        FlowControl _flowControl;
        OpCodeType _opCodeType;
        StackBehaviour _stackBehaviourPush;
        StackBehaviour _stackBehaviourPop;
    };
}