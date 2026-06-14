// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstring>

#include "../lib/loguru/loguru.hpp"

#include "../LibDescriptors/CapturedValueFlags.h"

#include "ArgumentCapture.h"

Profiler::ArgumentCapture::ArgumentCapture(ICorProfilerInfo10*& corProfilerInfo, LibProfiler::ObjectsTracker& objectsTracker) :
    _corProfilerInfo(corProfilerInfo),
    _objectsTracker(objectsTracker)
{
}

HRESULT Profiler::ArgumentCapture::GetArguments(
    const MethodDescriptor& methodDescriptor,
    std::vector<UINT_PTR>& indirects,
    const COR_PRF_FUNCTION_ARGUMENT_INFO& argumentInfos,
    std::vector<BYTE>& argumentValues,
    std::vector<BYTE>& argumentOffsets)
{
    for (auto&& argument : methodDescriptor.rewritingDescriptor.arguments)
    {
        auto const range = argumentInfos.ranges[argument.index];

        auto hr = GetArgument(argument, range, indirects, argumentValues, argumentOffsets);
        if (FAILED(hr))
        {
            LOG_F(ERROR, "Could not retrieve argument on index %d from method %s invocation.",
                argument.index,
                methodDescriptor.methodName.c_str());

            return E_FAIL;
        }
    }

    return S_OK;
}

HRESULT Profiler::ArgumentCapture::GetByRefArguments(
    const MethodDescriptor& methodDescriptor,
    const std::vector<UINT_PTR>& indirects,
    std::span<BYTE> indirectValues,
    std::span<BYTE> indirectOffsets)
{
    auto indirectsPointer = 0;
    for (const auto&[index, value] : methodDescriptor.rewritingDescriptor.arguments)
    {
        if ((static_cast<UINT>(value.flags) & static_cast<UINT>(CapturedValueFlags::IndirectLoad)) == 0)
            continue;

        UINT argInfo = index << 16 | value.size;
        const UINT_PTR indirectAddress = indirects[indirectsPointer];
        std::memcpy(indirectValues.data(), reinterpret_cast<LPVOID>(indirectAddress), value.size);
        std::memcpy(indirectOffsets.data(), &argInfo, sizeof(UINT));
        indirectValues = indirectValues.subspan(value.size);
        indirectOffsets = indirectOffsets.subspan(sizeof(UINT));
        indirectsPointer++;
    }

    return S_OK;
}

HRESULT Profiler::ArgumentCapture::GetArgument(
    const CapturedArgumentDescriptor& argument,
    const COR_PRF_FUNCTION_ARGUMENT_RANGE range,
    std::vector<UINT_PTR>& indirects,
    std::vector<BYTE>& argValues,
    std::vector<BYTE>& argOffsets)
{
    auto const flags = argument.value.flags;

    // Handle array of references
    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::CaptureAsReferenceArray)) != 0)
    {
        return GetReferenceArrayArgument(argument, range, argValues, argOffsets);
    }

    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::IndirectLoad)) != 0)
    {
        // Get pointer to the value
        UINT_PTR pointer;
        std::memcpy(&pointer, reinterpret_cast<LPVOID>(range.startAddress), sizeof(UINT_PTR));
        indirects.push_back(pointer);

        // Read the value
        auto const prevSize = argValues.size();
        argValues.resize(prevSize + argument.value.size);
        std::memcpy(argValues.data() + prevSize, reinterpret_cast<LPVOID>(pointer), argument.value.size);
        const UINT argInfo = (argument.index << 16) | argument.value.size;
        auto const prevOffsetSize = argOffsets.size();
        argOffsets.resize(prevOffsetSize + sizeof(UINT));
        std::memcpy(argOffsets.data() + prevOffsetSize, &argInfo, sizeof(UINT));
    }
    else
    {
        // Read the value
        auto const prevSize = argValues.size();
        argValues.resize(prevSize + range.length);
        const UINT argInfo = (argument.index << 16) | range.length;
        std::memcpy(argValues.data() + prevSize, reinterpret_cast<LPVOID>(range.startAddress), range.length);
        auto const prevOffsetSize = argOffsets.size();
        argOffsets.resize(prevOffsetSize + sizeof(UINT));
        std::memcpy(argOffsets.data() + prevOffsetSize, &argInfo, sizeof(UINT));
    }

    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::CaptureAsReference)) != 0)
    {
        // Managed reference (object can be later moved by GC)
        ObjectID objectId;
        std::memcpy(&objectId, argValues.data() + argValues.size() - sizeof(ObjectID), sizeof(ObjectID));
        auto const trackedObjectId = _objectsTracker.GetTrackedObject(objectId);
        std::memcpy(argValues.data() + argValues.size() - sizeof(ObjectID), &trackedObjectId, sizeof(ObjectID));
    }

    return S_OK;
}

HRESULT Profiler::ArgumentCapture::GetReferenceArrayArgument(
    const CapturedArgumentDescriptor& argument,
    const COR_PRF_FUNCTION_ARGUMENT_RANGE range,
    std::vector<BYTE>& argValues,
    std::vector<BYTE>& argOffsets)
{
    // Read the array ObjectID from the argument
    ObjectID arrayObjectId;
    std::memcpy(&arrayObjectId, reinterpret_cast<LPVOID>(range.startAddress), sizeof(ObjectID));

    if (arrayObjectId == 0)
    {
        // Null array: write count = 0
        auto const prevSize = argValues.size();
        argValues.resize(prevSize + sizeof(UINT));
        constexpr UINT count = 0;
        std::memcpy(argValues.data() + prevSize, &count, sizeof(UINT));

        constexpr UINT totalSize = sizeof(UINT);
        const UINT argInfo = (argument.index << 16) | totalSize;
        auto const prevOffsetSize = argOffsets.size();
        argOffsets.resize(prevOffsetSize + sizeof(UINT));
        std::memcpy(argOffsets.data() + prevOffsetSize, &argInfo, sizeof(UINT));
        return S_OK;
    }

    // Use GetArrayObjectInfo to get the array data pointer and dimensions
    ULONG32 dimensionSize = 0;
    int lowerBound = 0;
    BYTE* pData = nullptr;
    HRESULT hr = _corProfilerInfo->GetArrayObjectInfo(arrayObjectId, 1, &dimensionSize, &lowerBound, &pData);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not retrieve array object info. Error: 0x%x.", hr);
        return E_FAIL;
    }

    UINT const elementCount = dimensionSize;
    constexpr UINT elementSize = sizeof(LibProfiler::TrackedObjectId);
    UINT const totalSize = sizeof(UINT) + elementCount * elementSize;

    // Write: [4-byte count][N × tracked object IDs]
    auto const prevSize = argValues.size();
    argValues.resize(prevSize + totalSize);
    auto* writePtr = argValues.data() + prevSize;
    std::memcpy(writePtr, &elementCount, sizeof(UINT));
    writePtr += sizeof(UINT);

    // Read each element reference and resolve to tracked ID
    for (UINT i = 0; i < elementCount; i++)
    {
        ObjectID elementObjectId;
        std::memcpy(&elementObjectId, pData + i * sizeof(ObjectID), sizeof(ObjectID));

        LibProfiler::TrackedObjectId trackedId = 0;
        if (elementObjectId != 0)
        {
            trackedId = _objectsTracker.GetTrackedObject(elementObjectId);
        }

        std::memcpy(writePtr, &trackedId, elementSize);
        writePtr += elementSize;
    }

    // Write offset info
    const UINT argInfo = (argument.index << 16) | totalSize;
    auto const prevOffsetSize = argOffsets.size();
    argOffsets.resize(prevOffsetSize + sizeof(UINT));
    std::memcpy(argOffsets.data() + prevOffsetSize, &argInfo, sizeof(UINT));

    return S_OK;
}

HRESULT Profiler::ArgumentCapture::GetReturnValue(
    const CapturedValueDescriptor& value,
    const COR_PRF_FUNCTION_ARGUMENT_RANGE range,
    const std::span<BYTE>& returnValue)
{
    auto const flags = value.flags;

    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::CaptureAsReference)) != 0)
    {
        // Managed reference (object can be later moved by GC)
        ObjectID objectId;
        std::memcpy(&objectId, reinterpret_cast<LPVOID>(range.startAddress), sizeof(ObjectID));
        auto const trackedObjectId = _objectsTracker.GetTrackedObject(objectId);
        std::memcpy(returnValue.data(), &trackedObjectId, sizeof(ObjectID));
    }
    else
    {
        // Read the value
        std::memcpy(returnValue.data(), reinterpret_cast<LPVOID>(range.startAddress), range.length);
    }

    return S_OK;
}
