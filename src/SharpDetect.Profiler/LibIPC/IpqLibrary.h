// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "cor.h"

#include "../LibProfilerCore/PAL.h"

namespace LibIPC
{
	class IpqLibrary
	{
	public:
		explicit IpqLibrary(const std::string& libraryPath);
		~IpqLibrary() = default;
		IpqLibrary(const IpqLibrary&) = delete;
		IpqLibrary& operator=(const IpqLibrary&) = delete;
		IpqLibrary(IpqLibrary&&) = delete;
		IpqLibrary& operator=(IpqLibrary&&) = delete;

		[[nodiscard]] PVOID CreateProducer(
			const std::string& name,
			const std::string& file,
			const std::string& semaphore,
			INT size) const;
		void DestroyProducer(PVOID producer) const;
		[[nodiscard]] INT Enqueue(PVOID producer, BYTE* data, INT size) const;
		[[nodiscard]] INT RegisterProcess(const std::string& name, const std::string& file, INT size, INT pid) const;

		[[nodiscard]] PVOID CreateConsumer(
			const std::string& name,
			const std::string& file,
			const std::string& semaphore,
			INT size) const;
		void DestroyConsumer(PVOID consumer) const;
		[[nodiscard]] INT DequeueTimeout(PVOID consumer, BYTE** data, INT* size, INT timeoutMs) const;
		void FreeMemory(BYTE* data) const;

	private:
		using ipq_producer_create = PVOID(*)(const char*, const char*, const char*, INT);
		using ipq_producer_destroy = void (*)(PVOID);
		using ipq_producer_enqueue = INT(*)(PVOID, BYTE*, INT);
		using ipq_register_process = INT(*)(const char*, const char*, INT, INT);
		using ipq_consumer_create = PVOID(*)(const char*, const char*, const char*, INT);
		using ipq_consumer_destroy = void (*)(PVOID);
		using ipq_consumer_dequeue_timeout = INT(*)(PVOID, BYTE**, INT*, INT);
		using ipq_free_memory = void (*)(BYTE*);

		MODULE_HANDLE _moduleHandle;
		ipq_producer_create _producerCreate;
		ipq_producer_destroy _producerDestroy;
		ipq_producer_enqueue _producerEnqueue;
		ipq_register_process _registerProcess;
		ipq_consumer_create _consumerCreate;
		ipq_consumer_destroy _consumerDestroy;
		ipq_consumer_dequeue_timeout _consumerDequeueTimeout;
		ipq_free_memory _freeMemory;
	};
}
