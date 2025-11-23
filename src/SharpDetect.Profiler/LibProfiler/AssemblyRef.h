// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>
#include <utility>

#include "cor.h"
#include "corprof.h"

namespace LibProfiler
{
	class AssemblyRef
	{
	public:
		AssemblyRef(
			const mdAssemblyRef mdAssemblyRef,
			std::string name,
			const void* publicKeyData,
			const ULONG publicKeyDataLength,
			const DWORD flags) :
				_mdAssemblyRef(mdAssemblyRef),
				_name(std::move(name)),
				_publicKeyData(publicKeyData),
				_publicKeyDataLength(publicKeyDataLength),
				_flags(flags)
		{

		}

		[[nodiscard]] constexpr mdAssemblyRef GetMdAssemblyRef() const { return _mdAssemblyRef; }
		[[nodiscard]] const std::string& GetName() const { return _name; }
		[[nodiscard]] constexpr const void* GetPublicKeyData() const { return _publicKeyData; }
		[[nodiscard]] constexpr ULONG GetPublicKeyDataLength() const { return _publicKeyDataLength; }
		[[nodiscard]] constexpr DWORD GetFlags() const { return _flags; }

	private:
		mdAssemblyRef _mdAssemblyRef;
		std::string _name;
		const void* _publicKeyData;
		ULONG _publicKeyDataLength;
		DWORD _flags;
	};
}