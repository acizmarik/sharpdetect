// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "cor.h"
#include "corprof.h"

namespace LibProfiler
{
	class AssemblyRef
	{
	public:
		AssemblyRef(mdAssemblyRef mdAssemblyRef, std::string name, const void* publicKeyData, ULONG publicKeyDataLength, DWORD flags)
			: _mdAssemblyRef(mdAssemblyRef), _name(name), _publicKeyData(publicKeyData), _publicKeyDataLength(publicKeyDataLength), _flags(flags)
		{

		}

		const mdAssemblyRef GetMdAssemblyRef() const { return _mdAssemblyRef; }
		const std::string& GetName() const { return _name; }
		const void* GetPublicKeyData() const { return _publicKeyData; }
		const ULONG GetPublicKeyDataLength() const { return _publicKeyDataLength; }
		const DWORD GetFlags() const { return _flags; }

	private:
		mdAssemblyRef _mdAssemblyRef;
		std::string _name;
		const void* _publicKeyData;
		ULONG _publicKeyDataLength;
		DWORD _flags;
	};
}