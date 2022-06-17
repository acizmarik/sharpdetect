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
#include "PAL.h"

using namespace SharpDetect::Common::Messages;

Client::Client(el::Logger* pLogger)
	: pLogger(pLogger)
{
	auto notificationsProtocol = PAL::ReadEnvironmentVariable("SHARPDETECT_Notifications_Protocol");
	auto notificationsAddress = PAL::ReadEnvironmentVariable("SHARPDETECT_Notifications_Address");
	auto notificationsPort = PAL::ReadEnvironmentVariable("SHARPDETECT_Notifications_Port");
	auto requestsProtocol = PAL::ReadEnvironmentVariable("SHARPDETECT_Requests_Protocol");
	auto requestsAddress = PAL::ReadEnvironmentVariable("SHARPDETECT_Requests_Address");
	auto requestsPort = PAL::ReadEnvironmentVariable("SHARPDETECT_Requests_Port");
	auto responsesProtocol = PAL::ReadEnvironmentVariable("SHARPDETECT_Responses_Protocol");
	auto responsesAddress = PAL::ReadEnvironmentVariable("SHARPDETECT_Responses_Address");
	auto responsesPort = PAL::ReadEnvironmentVariable("SHARPDETECT_Responses_Port");
	notificationsEndpoint = notificationsProtocol + "://" + notificationsAddress + ":" + notificationsPort;
	requestsEndpoint = requestsProtocol + "://" + requestsAddress + ":" + requestsPort;
	responsesEndpoint = responsesProtocol + "://" + responsesAddress + ":" + responsesPort;

	notificationsThread = std::thread(&Client::PushWorker, 
		this, std::cref(notificationsEndpoint), std::ref(notificationsMutex), std::ref(queueNotifications), std::ref(cvNotifications));
	responsesThread = std::thread(&Client::PushWorker, 
		this, std::cref(responsesEndpoint), std::ref(responsesMutex), std::ref(queueResponses), std::ref(cvResponses));
	requestsThread = std::thread(&Client::RequestsWorker, this);
}

Client::~Client()
{

}

void Client::RequestsWorker()
{
	// Setup socket
	zmq::socket_t requestsSocket(context, zmq::socket_type::sub);
	requestsSocket.set(zmq::sockopt::subscribe, std::to_string(PAL::GetProcessId()));
	requestsSocket.connect(requestsEndpoint);

	// Prepare zmq_pollitem for poller
	// TODO: update to newer CPPZMQ API once available
	const auto pollTimeoutMillis = 1000;
	auto zmqItem = zmq_pollitem_t();
	zmqItem.socket = requestsSocket;
	zmqItem.fd = 0;
	zmqItem.revents = 0;
	zmqItem.events = ZMQ_POLLIN;

	while (!finish)
	{
		// Poll for items with fixed timeout
		zmq_poll(&zmqItem, 1, pollTimeoutMillis);
		if (!(zmqItem.revents & ZMQ_POLLIN))
			continue;

		// Read next request
		zmq::message_t message;

		// Discard topic
		requestsSocket.recv(message);
		// Receive actual payload
		if (requestsSocket.recv(message))
		{
			auto request = RequestMessage();
			request.ParseFromArray(message.data(), message.size());

			// Prepare response
			auto const notificationId = request.notificationid();
			auto const requestId = request.requestid();

			if (request.Payload_case() == RequestMessage::PayloadCase::kPing)
			{
				// If this is a ping, immediately follow with a reponse
				SendResponse(MessageFactory::RequestProcessed(0, requestId, true), requestId);
			}
			else
			{
				// Non-trivial request needs to be processed first
				auto lock = std::unique_lock<std::mutex>(promisesMutex);
				// Check if there is a promise for this request
				if (promises.find(notificationId) != promises.cend())
				{
					promises[request.notificationid()].set_value(std::move(request));
					promises.erase(notificationId);
				}
				else
				{
					LOG_ERROR(pLogger, "Communication protocol error: could not match request to an existing promise!\n");
				}
			}
		}
	}
}

void Client::PushWorker(const std::string& endpoint, std::mutex& queueMutex, std::queue<NotifyMessage>& queue, std::condition_variable& cv)
{
	auto length = 65536;
	auto buffer = std::make_unique<char[]>(length);

	zmq::socket_t socket(context, zmq::socket_type::push);
	socket.connect(endpoint);

	while (!finish)
	{
		auto lock = std::unique_lock<std::mutex>(queueMutex);
		// Wait while queue empty
		while (queue.size() == 0 && !finish)
			cv.wait(lock);

		// Process enqueued notifications
		while (queue.size() > 0)
		{
			auto current = std::move(queue.front());

			// Marshall to byte array
			const auto size = current.ByteSizeLong();
			if (size >= length)
			{
				length = GetNewBufferSize(length, size);
				buffer = std::make_unique<char[]>(length);
			}

			current.SerializeToArray(buffer.get(), size);

			// Send notification
			socket.send(buffer.get(), size, 0);
			queue.pop();
		}
	}
}

size_t Client::GetNewBufferSize(size_t current, size_t minRequestSize)
{
	do current *= 2;
	while (current <= minRequestSize);
	return current;
}

void Client::Shutdown()
{
	finish = true;

	// Requests thread periodically unblocks to check finish flag
	requestsThread.join();

	// Responses thread needs to be notified to finish
	cvResponses.notify_one();
	responsesThread.join();

	// Notifications thread needs to be notified to finish
	cvNotifications.notify_one();
	notificationsThread.join();
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

void Client::SendResponse(SharpDetect::Common::Messages::NotifyMessage&& response, uint64_t requestId)
{
	{
		// Enqueue response & notify consumer
		auto lock = std::unique_lock<std::mutex>(responsesMutex);
		queueResponses.push(std::move(response));
		cvResponses.notify_one();
	}
}

std::future<RequestMessage> Client::ReceiveRequest(uint64_t notificationId)
{
	auto lock = std::unique_lock<std::mutex>(promisesMutex);
	promises.insert(std::make_pair(notificationId, std::promise<RequestMessage>()));

	return promises[notificationId].get_future();
}
