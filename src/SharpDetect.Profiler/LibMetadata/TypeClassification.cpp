// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "TypeClassification.h"

#include <vector>

LibProfiler::GenericTypeArgKind LibProfiler::ClassifyClass(
	ICorProfilerInfo2& corProfilerInfo,
	const ModuleDefResolver& resolveModuleDef,
	const ClassID classId)
{
	if (classId == 0)
		return GenericTypeArgKind::Unknown;

	CorElementType baseElemType;
	if (corProfilerInfo.IsArrayClass(classId, &baseElemType, nullptr, nullptr) == S_OK)
		return GenericTypeArgKind::Reference;

	ModuleID moduleId = 0;
	mdTypeDef typeDef = 0;
	ClassID parentClassId = 0;
	ULONG32 numTypeArgs = 0;
	if (FAILED(corProfilerInfo.GetClassIDInfo2(classId, &moduleId, &typeDef, &parentClassId, 0, &numTypeArgs, nullptr)))
		return GenericTypeArgKind::Unknown;

	// System.Object
	if (parentClassId == 0)
		return GenericTypeArgKind::Reference;

	// A value type's immediate base is always System.ValueType - single parent check should be enough
	ModuleID parentModuleId = 0;
	mdTypeDef parentTypeDef = 0;
	ULONG32 numParentTypeArgs = 0;
	if (FAILED(corProfilerInfo.GetClassIDInfo2(parentClassId, &parentModuleId, &parentTypeDef, nullptr, 0, &numParentTypeArgs, nullptr)))
		return GenericTypeArgKind::Unknown;

	const auto parentModule = resolveModuleDef(parentModuleId);
	if (parentModule == nullptr)
		return GenericTypeArgKind::Unknown;

	std::string parentName;
	mdToken parentExtends = mdTokenNil;
	if (FAILED(parentModule->GetTypeProps(parentTypeDef, &parentExtends, parentName)))
		return GenericTypeArgKind::Unknown;

	if (parentName == "System.ValueType" || parentName == "System.Enum")
		return GenericTypeArgKind::Value;

	return GenericTypeArgKind::Reference;
}

LibProfiler::GenericTypeArgKind LibProfiler::ClassifyClassGenericArgument(
	ICorProfilerInfo2& corProfilerInfo,
	const ModuleDefResolver& resolveModuleDef,
	const FunctionID functionId,
	const COR_PRF_FRAME_INFO frameInfo,
	const ULONG32 typeArgIndex)
{
	ClassID classId = 0;
	ModuleID moduleId = 0;
	mdToken token = 0;
	ULONG32 numMethodTypeArgs = 0;

	if (FAILED(corProfilerInfo.GetFunctionInfo2(functionId, frameInfo, &classId, &moduleId, &token, 0, &numMethodTypeArgs, nullptr)) || classId == 0)
		return GenericTypeArgKind::Unknown;

	ULONG32 numClassTypeArgs = 0;
	if (FAILED(corProfilerInfo.GetClassIDInfo2(classId, nullptr, nullptr, nullptr, 0, &numClassTypeArgs, nullptr)) ||
		typeArgIndex >= numClassTypeArgs)
		return GenericTypeArgKind::Unknown;

	std::vector<ClassID> classTypeArgs(numClassTypeArgs);
	if (FAILED(corProfilerInfo.GetClassIDInfo2(classId, nullptr, nullptr, nullptr, numClassTypeArgs, &numClassTypeArgs, classTypeArgs.data())))
		return GenericTypeArgKind::Unknown;

	return ClassifyClass(corProfilerInfo, resolveModuleDef, classTypeArgs[typeArgIndex]);
}
