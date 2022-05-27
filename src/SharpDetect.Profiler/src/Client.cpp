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

#include "Stdafx.h"
#include "Client.h"

using namespace SharpDetect::Common::Messages;

void Client::NotificationsWorker()
{
	auto length = 65536;
	auto buffer = std::make_unique<char[]>(length);

	auto notificationsSocket = zmq_socket(pContext, ZMQ_PUSH);
	auto endpoint = notificationsAddress + ":" + std::to_string(notificationsPort);
	zmq_setsockopt(notificationsSocket, ZMQ_LINGER, "", -1);
	zmq_connect(notificationsSocket, endpoint.c_str());

	while (!finish)
	{		
		auto lock = std::unique_lock<std::mutex>(notificationsMutex);			
		// Wait while queue empty
		while (queueNotifications.size() == 0 && !finish)
			cvNotifications.wait(lock);

		// Process enqueued notifications
		while (queueNotifications.size() > 0)
		{
			auto current = std::move(queueNotifications.front());

			// Marshall to byte array
			const auto size = current.ByteSizeLong();
			if (size >= length)
			{
				length = GetNewBufferSize(length, size);
				buffer = std::make_unique<char[]>(length);
			}

			current.SerializeToArray(buffer.get(), size);

			// Send notification
			zmq_send(notificationsSocket, buffer.get(), size, 0);
			queueNotifications.pop();
		}
	}
	zmq_close(notificationsSocket);
}

void Client::RequestsWorker()
{
	const auto pollTimeoutMillis = 1000;
	auto length = 65536;
	auto buffer = std::make_unique<char[]>(length);

	auto requestsSocket = zmq_socket(pContext, ZMQ_REP);
	auto endpoint = requestsAddress + ":" + std::to_string(requestsPort);
	zmq_bind(requestsSocket, endpoint.c_str());
	zmq_setsockopt(requestsSocket, ZMQ_LINGER, "", -1);

	// Prepare zmq_pollitem for poller
	auto zmqItem = zmq_pollitem_t();
	zmqItem.socket = requestsSocket;
	zmqItem.fd = 0;
	zmqItem.revents = 0;
	zmqItem.events = ZMQ_POLLIN;

	while (!finish)
	{
		// Poll for items with fixed timeout
		auto result = zmq_poll(&zmqItem, 1, pollTimeoutMillis);
		if (!(zmqItem.revents & ZMQ_POLLIN))
			continue;

		// Read next request
		auto read = zmq_recv(requestsSocket, buffer.get(), length, 0);

		// Parse request request
		auto request = RequestMessage();
		auto requestResult = false;	
		request.ParseFromArray(buffer.get(), read);

		// Prepare response
		auto const notificationId = request.notificationid();
		auto const requestId = request.requestid();
		{
			auto lock = std::unique_lock<std::mutex>(promisesMutex);
			// Check if there is a promise for this request
			if (promises.find(notificationId) != promises.cend())
			{
				promises[request.notificationid()].set_value(std::move(request));
				promises.erase(notificationId);
				requestResult = true;
			}
			else
			{
				LOG_INFO(pLogger, "Communication protocol error: could not match request to an existing promise!\n");
				requestResult = false;
			}
		}

		// Send response
		auto response = MessageFactory::RequestProcessed(0, requestId, result);
		const auto responseLength = response.ByteSizeLong();
		if (responseLength > length)
		{
			length = GetNewBufferSize(length, responseLength);
			buffer = std::make_unique<char[]>(length);
		}

		response.SerializeToArray(buffer.get(), responseLength);
		zmq_send(requestsSocket, buffer.get(), responseLength, 0);
	}
	zmq_close(requestsSocket);
}

size_t Client::GetNewBufferSize(size_t current, size_t minRequestSize)
{
	do current *= 2;
	while (current <= minRequestSize);
	return current;
}

Client::Client(el::Logger* pLogger, int notificationsPort, int requestsPort)
	: pLogger(pLogger), notificationsPort(notificationsPort), requestsPort(requestsPort)
{
	pContext = zmq_ctx_new();
	notificationsPort = (notificationsPort > 0) ? notificationsPort : 1111;
	requestsPort = (requestsPort > 0) ? requestsPort : 1112;
	notificationsThread = std::thread(&Client::NotificationsWorker, this);
	requestsThread = std::thread(&Client::RequestsWorker, this);
}

Client::~Client()
{
	
}

void Client::Shutdown()
{
	finish = true;

	// Requests thread periodically unblocks to check finish flag
	requestsThread.join();

	// Notifications thread needs to be notified to finish
	cvNotifications.notify_one();
	notificationsThread.join();

	zmq_ctx_shutdown(pContext);
	zmq_ctx_destroy(pContext);
}

const uint64_t Client::SendNotification(NotifyMessage&& message)
{
	return SendNotification(std::move(message), GetNewNotificationId());
}

const uint64_t Client::SendNotification(NotifyMessage&& message, uint64_t id)
{
	message.set_notificationid(id);
	{
		// Enqueue job & notify consumer
		auto lock = std::unique_lock<std::mutex>(notificationsMutex);
		queueNotifications.push(std::move(message));
		cvNotifications.notify_one();
	}

	return id;
}

std::future<RequestMessage> Client::ReceiveRequest(uint64_t notificationId)
{
	auto lock = std::unique_lock<std::mutex>(promisesMutex);
	promises.insert(std::make_pair(notificationId, std::promise<RequestMessage>()));

	return promises[notificationId].get_future();
}
