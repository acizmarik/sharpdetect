-- Copyright 2023 Andrej Čižmárik and Contributors
-- SPDX-License-Identifier: Apache-2.0

-- Common module for assembly descriptors
local sd = { }

function sd.flagsOr(table)
	local result = table[1]
	local index = 2
	while table[index] do
		result = result .. table[index]
		index = index + 1
	end

	return result
end

function sd.createMethodRecord(identifier, interpretation)
	return MethodRecord(identifier, interpretation)
end

function sd.createMethodIdentifier(name, declaringType, isStatic, argc, argTypes, isInjected)
	return MethodIdentifier(name, declaringType, isStatic, argc, argTypes, isInjected)
end

function sd.createMethodInterpretation(type, flags, capturedArgs, checker)
	return MethodInterpretationData.__new(type, flags, capturedArgs, checker)
end

function sd.createCapturedParameterInfo(index, size, isIndirectLoad)
	return CapturedParameterInfo(index, size, isIndirectLoad)
end

function sd.createExpressionBuilder()
	return CSharpExpressionBuilder.__new()
end

return sd;