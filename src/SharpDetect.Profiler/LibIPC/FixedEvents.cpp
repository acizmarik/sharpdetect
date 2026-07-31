// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstring>

#include "FixedEvents.h"

namespace
{
	void AppendBytes(std::vector<char>& buffer, const void* data, const std::size_t size)
	{
		if (size == 0)
			return;

		const auto offset = buffer.size();
		buffer.resize(offset + size);
		std::memcpy(buffer.data() + offset, data, size);
	}

	template<typename T>
	void Append(std::vector<char>& buffer, const T value)
	{
		AppendBytes(buffer, &value, sizeof(T));
	}

	void AppendBlob(std::vector<char>& buffer, const LibIPC::ByteSpanView blob)
	{
		AppendBytes(buffer, blob.data, blob.size);
	}

	UINT32 BlobLength(const LibIPC::ByteSpanView blob)
	{
		return static_cast<UINT32>(blob.size);
	}

	void WriteHeader(
		std::vector<char>& buffer,
		const LibIPC::RecordedEventType type,
		const UINT64 threadId,
		const UINT64 moduleId,
		const UINT32 methodToken,
		const USHORT interpretation)
	{
		buffer.clear();
		Append(buffer, static_cast<BYTE>(type));
		Append(buffer, threadId);
		Append(buffer, moduleId);
		Append(buffer, methodToken);
		Append(buffer, interpretation);
	}
}

void LibIPC::FixedEvents::WriteMethodEnter(
	std::vector<char>& buffer,
	const UINT64 threadId,
	const UINT64 moduleId,
	const UINT32 methodToken,
	const USHORT interpretation)
{
	WriteHeader(buffer, RecordedEventType::MethodEnter, threadId, moduleId, methodToken, interpretation);
}

void LibIPC::FixedEvents::WriteMethodExit(
	std::vector<char>& buffer,
	const UINT64 threadId,
	const UINT64 moduleId,
	const UINT32 methodToken,
	const USHORT interpretation)
{
	WriteHeader(buffer, RecordedEventType::MethodExit, threadId, moduleId, methodToken, interpretation);
}

void LibIPC::FixedEvents::WriteMethodEnterWithArguments(
	std::vector<char>& buffer,
	const UINT64 threadId,
	const UINT64 moduleId,
	const UINT32 methodToken,
	const USHORT interpretation,
	const ByteSpanView argumentValues,
	const ByteSpanView argumentInfos,
	const std::optional<ByteSpanView> stackFrames)
{
	WriteHeader(buffer, RecordedEventType::MethodEnterWithArguments, threadId, moduleId, methodToken, interpretation);
	Append(buffer, BlobLength(argumentValues));
	Append(buffer, BlobLength(argumentInfos));
	Append(buffer, stackFrames.has_value() ? BlobLength(*stackFrames) : AbsentBlob);
	AppendBlob(buffer, argumentValues);
	AppendBlob(buffer, argumentInfos);
	if (stackFrames.has_value())
		AppendBlob(buffer, *stackFrames);
}

void LibIPC::FixedEvents::WriteMethodExitWithArguments(
	std::vector<char>& buffer,
	const UINT64 threadId,
	const UINT64 moduleId,
	const UINT32 methodToken,
	const USHORT interpretation,
	const ByteSpanView returnValue,
	const ByteSpanView byRefArgumentValues,
	const ByteSpanView byRefArgumentInfos)
{
	WriteHeader(buffer, RecordedEventType::MethodExitWithArguments, threadId, moduleId, methodToken, interpretation);
	Append(buffer, BlobLength(returnValue));
	Append(buffer, BlobLength(byRefArgumentValues));
	Append(buffer, BlobLength(byRefArgumentInfos));
	AppendBlob(buffer, returnValue);
	AppendBlob(buffer, byRefArgumentValues);
	AppendBlob(buffer, byRefArgumentInfos);
}
