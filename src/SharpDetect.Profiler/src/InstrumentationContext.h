/*
 * Copyright (C) 2020, Andrej Čižmárik
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#ifndef INSTRUMENTATIONCONTEXT_HEADER_GUARD
#define INSTRUMENTATIONCONTEXT_HEADER_GUARD

#include "cor.h"
#include "corprof.h"
#include "Client.h"
#include "Logging.h"
#include "MessageFactory.h"
#include "TinyMethodUser.h"
#include "ModuleMetadata.h"
#include "profiler_notifications.pb.h"

#include <unordered_map>
#include <vector>
#include <mutex>
#include <tuple>

class InstrumentationContext
{
public:

	InstrumentationContext(Client& client, ICorProfilerInfo8& corProfilerInfo, el::Logger& logger)
		: client(client), corProfilerInfo(corProfilerInfo), logger(logger), mdTokenEventsDispatcherType(mdTypeDefNil), 
		mdTokenFieldAccessMethod(mdMethodDefNil), mdTokenFieldInstanceRefAccessMethod(mdMethodDefNil), 
		mdTokenArrayElementAccessMethod(mdMethodDefNil), mdTokenArrayInstanceRefAccessMethod(mdMethodDefNil), mdTokenArrayIndexAccessMethod(mdMethodDefNil),
		corLibPublicKey(nullptr), corLibPublicKeySize(-1), corLibFlags(0), corLibMetadata({})
	{

	}

	const WSTRING& GetCoreLibraryName() const
	{
		static const auto coreLibrary = WSTRING("System.Private.CoreLib.dll"_W);
		return coreLibrary;
	}

	ThreadID GetCurrentThreadId() const
	{
		auto threadId = ThreadID(0);
		corProfilerInfo.GetCurrentThreadID(&threadId);
		return threadId;
	}

	HRESULT CreateHelperMethods(ModuleMetadata& coreLibMetadata);
	HRESULT CreateHelperMethod(ModuleMetadata& coreLibMetadata, const WSTRING& name, SharpDetect::Common::Messages::MethodType type, const std::vector<COR_SIGNATURE>& signature, ULONG cbSignature, mdToken& newToken);
	HRESULT WrapExternMethod(ModuleMetadata& metadata, mdTypeDef typeToken, mdMethodDef methodToken, UINT16 parametersCount);
	
	HRESULT ImportWrappers(ModuleMetadata& metadata);
	HRESULT ImportHelpers(ModuleMetadata& metadata);
	HRESULT ImportCoreLibInfo(ModuleMetadata& coreLibMetadata);

private:
	using WrappedMethodInfo = std::tuple<ModuleID, mdTypeDef, mdMethodDef, PCCOR_SIGNATURE, ULONG>;
	using WrappedMethodsCollection = std::unordered_map<WSTRING, WrappedMethodInfo>;
	using HelperMethodInfo = std::tuple<WSTRING, SharpDetect::Common::Messages::MethodType, std::vector<COR_SIGNATURE>, ULONG>;

	Client& client;
	el::Logger& logger;

	ICorProfilerInfo8& corProfilerInfo;
	mdTypeDef mdTokenEventsDispatcherType;
	mdMethodDef mdTokenFieldAccessMethod;
	mdMethodDef mdTokenFieldInstanceRefAccessMethod;
	mdMethodDef mdTokenArrayElementAccessMethod;
	mdMethodDef mdTokenArrayInstanceRefAccessMethod;
	mdMethodDef mdTokenArrayIndexAccessMethod;

	const void* corLibPublicKey;
	ULONG corLibPublicKeySize;
	DWORD corLibFlags;
	ASSEMBLYMETADATA corLibMetadata;

	std::vector<HelperMethodInfo> helpers;
	std::unordered_map<WSTRING, std::unordered_map<WSTRING, WrappedMethodsCollection>> wrappers;
	std::mutex wrappersMutex;
	std::mutex helpersMutex;
};

#endif