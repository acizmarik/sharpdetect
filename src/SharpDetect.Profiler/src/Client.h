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

#ifndef CLIENT_HEADER_GUARD
#define CLIENT_HEADER_GUARD

#include "MessageFactory.h"
#include <zmq.hpp>
#include <cstdint>
#include <memory>
#include <atomic>
#include <thread>
#include <mutex>
#include <queue>
#include <functional>
#include <future>
#include <string>
#include <unordered_map>
#include <condition_variable>
#include "sal.h"
#include "Logging.h"
#include "profiler_notifications.pb.h"
#include "profiler_requests.pb.h"

class Client
{
public:
	Client(el::Logger* pLogger);
	~Client();

	Client(const Client& client) = delete;
	Client& operator=(const Client& client) = delete;
	Client(Client&& client) = delete;
	Client& operator=(Client&& client) = delete;

	const uint64_t GetNewNotificationId()
	{
		static std::atomic<uint64_t> id {0};
		return ++id;
	}

	const uint64_t SendNotification(SharpDetect::Common::Messages::NotifyMessage&& message);
	const uint64_t SendNotification(SharpDetect::Common::Messages::NotifyMessage&& message, uint64_t id);
	void SendResponse(SharpDetect::Common::Messages::NotifyMessage&& response, uint64_t requestId);

	void Shutdown();

	std::future<SharpDetect::Common::Messages::RequestMessage> ReceiveRequest(uint64_t notificationId);

private:
	static inline const std::string notificationsInternalEndpoint = "inproc://profiling-notifications";
	static inline const std::string responsesInternalEndpoint = "inproc://profiling-responses";

	void PushWorker(const std::string& inEndpoint, const std::string& outEndpoint);
	void RequestsWorker();
	void SignalsWorker();

	std::string signalsEndpoint;
	std::string notificationsEndpoint;
	std::string requestsEndpoint;
	std::string responsesEndpoint;

	el::Logger* pLogger;
	zmq::context_t context;
	std::thread notificationsThread;
	std::thread requestsThread;
	std::thread responsesThread;
	std::thread signalsThread;
	std::mutex promisesMutex;
	std::unordered_map<uint64_t, std::promise<SharpDetect::Common::Messages::RequestMessage>> promises;
	volatile bool finish = false;
};

#endif