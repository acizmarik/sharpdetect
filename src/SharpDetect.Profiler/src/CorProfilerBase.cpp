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

#include "CorProfilerBase.h"

CorProfilerBase::CorProfilerBase()
	: pCorProfilerInfo(nullptr), pMessagingClient(nullptr), pLogger(nullptr), refCount(0)
{
	PrepareLogging();
	PrepareMessagingClient();
	instance = this;
}

CorProfilerBase::~CorProfilerBase()
{
	if (this->pCorProfilerInfo != nullptr)
	{
		this->pCorProfilerInfo->Release();
		this->pCorProfilerInfo = nullptr;
	}
}

void CorProfilerBase::PrepareLogging()
{
	pLogger = el::Loggers::getLogger("default");
	auto configurations = el::Configurations();
	configurations.setGlobally(el::ConfigurationType::Enabled, "true");
	configurations.setGlobally(el::ConfigurationType::ToFile, "true");
	configurations.setGlobally(el::ConfigurationType::MaxLogFileSize, "16384");
	configurations.setGlobally(el::ConfigurationType::ToStandardOutput, "false");
	configurations.setGlobally(el::ConfigurationType::Filename, "log.txt");
	configurations.set(el::Level::Warning, el::ConfigurationType::Format, "%datetime %level %msg");
	configurations.set(el::Level::Error, el::ConfigurationType::Format, "%datetime %level %msg");
	configurations.set(el::Level::Fatal, el::ConfigurationType::Format, "%datetime %level %msg");
	pLogger->configure(configurations);
}

void CorProfilerBase::PrepareMessagingClient()
{
	auto notifications = -1;
	auto requests = -1;

	// Get notifications port
	if (const auto notificationsPort = std::getenv("SHARPDETECT_PROFILER_NOTIFICATIONS_PORT"))
	{
		auto stringStream = std::stringstream(notificationsPort);
		stringStream >> notifications;
	}
	// Get requests port
	if (const auto requestsPort = std::getenv("SHARPDETECT_PROFILER_REQUESTS_PORT"))
	{
		auto stringStream = std::stringstream(requestsPort);
		stringStream >> requests;
	}

	// Initialize messaging client
	notifications = (notifications > 0) ? notifications : Client::GetDefaultNotificationsPort();
	requests = (requests > 0) ? requests : Client::GetDefaultRequestsPort();
	pMessagingClient = std::make_unique<Client>(pLogger, notifications, requests);
}

const FunctionInfo* CorProfilerBase::TryGetHookData(FunctionID function)
{
	std::lock_guard<std::mutex> lock(hooksDataMutex);
	if (injectedHooks.find(function) == injectedHooks.cend())
		return nullptr;

	return injectedHooks.at(function).get();
}

void CorProfilerBase::RegisterHook(FunctionID function, ModuleID module, mdTypeDef classToken, mdMethodDef functionToken,
	bool captureArguments, bool captureReturnValue, size_t totalArgumentsSize, size_t totalIndirectArgumentSize,
	std::vector<std::tuple<UINT16, UINT16, bool>>&& argumentInfos)
{
	auto hookDataStorage = new FunctionInfo();
	hookDataStorage->ModuleId = module;
	hookDataStorage->ClassToken = classToken;
	hookDataStorage->FunctionToken = functionToken;
	hookDataStorage->CaptureArguments = captureArguments;
	hookDataStorage->CaptureReturnValue = captureReturnValue;
	hookDataStorage->TotalArgumentValuesSize = totalArgumentsSize;
	hookDataStorage->TotalIndirectArgumentValuesSize = totalIndirectArgumentSize;
	hookDataStorage->ArgumentInfos = std::move(argumentInfos);

	{
		std::lock_guard<std::mutex> lock(hooksDataMutex);
		injectedHooks.emplace(std::make_pair(function, std::unique_ptr<FunctionInfo>(hookDataStorage)));
	}
}

HRESULT STDMETHODCALLTYPE CorProfilerBase::Initialize(IUnknown* pICorProfilerInfoUnk)
{
	auto hr = pICorProfilerInfoUnk->QueryInterface(__uuidof(ICorProfilerInfo8), reinterpret_cast<void**>(&this->pCorProfilerInfo));
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not query ICorProfilerInfo8.");

	return hr;
}

HRESULT STDMETHODCALLTYPE CorProfilerBase::Shutdown()
{
	if (this->pCorProfilerInfo != nullptr)
	{
		this->pCorProfilerInfo->Release();
		this->pCorProfilerInfo = nullptr;
	}

	return S_OK;
}

CorProfilerBase* CorProfilerBase::instance = nullptr;