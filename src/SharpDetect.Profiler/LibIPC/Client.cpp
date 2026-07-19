// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstdlib>
#include <stdexcept>
#include <utility>
#include <vector>

#include "../lib/loguru/loguru.hpp"
#include "../LibProfilerCore/PAL.h"

#include "Client.h"
#include "Messages.h"

LibIPC::Client::Client(
    const QueueEndpoint& commandQueue,
    const QueueEndpoint& eventQueue,
    const RegistrationEndpoint& registrationQueue) :
	_commandReceivingEnabled(true),
	_shutdownCompleted(false)
{
	auto const ipqPathStringPointer = std::getenv("SharpDetect_IPQ_PATH");
	if (ipqPathStringPointer == nullptr)
	{
		LOG_F(FATAL, "Could not obtain path to IPQ library.");
		throw std::runtime_error("Error while configuring memory mapped file.");
	}
	auto const ipqPath = std::string(ipqPathStringPointer);

	std::size_t eventQueueMaxBytes = 64 * 1024 * 1024;
	if (auto const eventBufferMaxStringPointer = std::getenv("SharpDetect_EVENT_BUFFER_MAX_BYTES"))
	{
		try
		{
			auto const parsed = std::stoull(eventBufferMaxStringPointer);
			if (parsed > 0)
				eventQueueMaxBytes = static_cast<std::size_t>(parsed);
		}
		catch (const std::exception&)
		{
			LOG_F(WARNING, "Could not parse SharpDetect_EVENT_BUFFER_MAX_BYTES=%s; using default.", eventBufferMaxStringPointer);
		}
	}

	_library = std::make_unique<IpqLibrary>(ipqPath);

	// Create producer for events
	LOG_F(INFO, "IPC event worker configuration: { name: %s, file: %s, size: %d }", eventQueue.name.c_str(), eventQueue.file.c_str(), eventQueue.size);
	_producer = std::make_unique<IpqProducer>(*_library, eventQueue.name, eventQueue.file, eventQueue.semaphoreName, static_cast<INT>(eventQueue.size));

	const auto currentPid = static_cast<INT>(LibProfiler::PAL_GetCurrentPid());
	LOG_F(INFO, "Registering process %d via table: { name: %s, file: %s, size: %d }", currentPid, registrationQueue.name.c_str(), registrationQueue.file.c_str(), registrationQueue.size);
	const INT registrationResult = _library->RegisterProcess(registrationQueue.name, registrationQueue.file, static_cast<INT>(registrationQueue.size), currentPid);
	if (registrationResult != 0)
	{
		LOG_F(FATAL, "Communication library could not register process %d (error %d).", currentPid, registrationResult);
		throw std::runtime_error("Could not register process with IPC registration table.");
	}

	// Create consumer for commands
	LOG_F(INFO, "IPC command worker configuration: { name: %s, file: %s, size: %d }", commandQueue.name.c_str(), commandQueue.file.c_str(), commandQueue.size);
	_consumer = std::make_unique<IpqConsumer>(*_library, commandQueue.name, commandQueue.file, commandQueue.semaphoreName, static_cast<INT>(commandQueue.size));

	_events = std::make_unique<EventDispatcher>(*_producer, eventQueueMaxBytes);
	_commands = std::make_unique<CommandDispatcher>(*_consumer);

	LOG_F(INFO, "Communication library initialized with command receiving enabled.");
	_events->Start();
	_commands->Start();
}

LibIPC::Client::~Client()
{
	Shutdown();
}

void LibIPC::Client::Shutdown()
{
	if (_producer == nullptr)
		return;

	if (_shutdownCompleted.exchange(true, std::memory_order_acq_rel))
		return;

	_events->Stop();
	if (_commandReceivingEnabled)
		_commands->Stop();

	// Notify managed that we are gracefully terminating
	const auto destroyMsg = Helpers::CreateProfilerDestroyMsg(
		Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), 0));
	msgpack::sbuffer sbuf;
	msgpack::pack(sbuf, destroyMsg);
	std::vector<char> buffer(sbuf.data(), sbuf.data() + sbuf.size());
	_producer->Send(buffer);
}
