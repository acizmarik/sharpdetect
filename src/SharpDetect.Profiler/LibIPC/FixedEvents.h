// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstddef>
#include <optional>
#include <vector>

#include "cor.h"
#include "Messages.h"

namespace LibIPC
{
	namespace FixedEvents
	{
		// [u64 threadId][u64 moduleId][u32 methodToken][u16 interpretation]
		// The process id and command id are not on wire
		constexpr std::size_t HeaderSize = 22;
		constexpr BYTE MsgPackFormat = 0;
		constexpr UINT32 AbsentBlob = 0xFFFFFFFFu;

		void WriteMethodEnter(
			std::vector<char>& buffer,
			UINT64 threadId,
			UINT64 moduleId,
			UINT32 methodToken,
			USHORT interpretation);

		void WriteMethodExit(
			std::vector<char>& buffer,
			UINT64 threadId,
			UINT64 moduleId,
			UINT32 methodToken,
			USHORT interpretation);

		void WriteMethodEnterWithArguments(
			std::vector<char>& buffer,
			UINT64 threadId,
			UINT64 moduleId,
			UINT32 methodToken,
			USHORT interpretation,
			ByteSpanView argumentValues,
			ByteSpanView argumentInfos,
			std::optional<ByteSpanView> stackFrames);

		void WriteMethodExitWithArguments(
			std::vector<char>& buffer,
			UINT64 threadId,
			UINT64 moduleId,
			UINT32 methodToken,
			USHORT interpretation,
			ByteSpanView returnValue,
			ByteSpanView byRefArgumentValues,
			ByteSpanView byRefArgumentInfos);
	}
}
