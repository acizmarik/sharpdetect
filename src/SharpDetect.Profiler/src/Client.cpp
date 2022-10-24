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
#include <chrono>

using namespace SharpDetect::Common::Messages;

Client::Client(el::Logger* pLogger)
	: pLogger(pLogger)
{
	auto signalsProtocol = PAL::ReadEnvironmentVariable("SHARPDETECT_Signals_Protocol");
	auto signalsAddress = PAL::ReadEnvironmentVariable("SHARPDETECT_Signals_Address");
	auto signalsPort = PAL::ReadEnvironmentVariable("SHARPDETECT_Signals_Port");
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
	signalsEndpoint = signalsProtocol + "://" + signalsAddress + ":" + signalsPort;

	notificationsThread = std::thread(&Client::PushWorker, 
		this, std::cref(notificationsInternalEndpoint), std::cref(notificationsEndpoint));
	responsesThread = std::thread(&Client::PushWorker, 
		this, std::cref(responsesInternalEndpoint), std::cref(responsesEndpoint));
	requestsThread = std::thread(&Client::RequestsWorker, this);
	signalsThread = std::thread(&Client::SignalsWorker, this);
}

Client::~Client()
{

}

void Client::SignalsWorker()
{
	auto size = MessageFactory::Heartbeat().ByteSizeLong();
	auto buffer = std::make_unique<char[]>(size);

	zmq::socket_t socket(context, zmq::socket_type::push);
	socket.connect(signalsEndpoint);

	while (!finish)
	{
		// Send heartbeat
		auto&& heartbeat = MessageFactory::Heartbeat();
		heartbeat.SerializeToArray(buffer.get(), size);

		// Send notification
		socket.send(buffer.get(), size, 0);

		// Put thread to sleep until next heartbeat
		std::this_thread::sleep_for(std::chrono::milliseconds(1500));
	}
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
		auto _ = requestsSocket.recv(message);
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

void Client::PushWorker(const std::string& inEndpoint, const std::string& outEndpoint)
{
	zmq::socket_t inSocket(context, zmq::socket_type::pull);
	zmq::socket_t outSocket(context, zmq::socket_type::push);
	inSocket.bind(inEndpoint);
	outSocket.connect(outEndpoint);

	// Prepare zmq_pollitem for poller
	const auto pollTimeoutMillis = 1000;
	auto zmqItem = zmq_pollitem_t();
	zmqItem.socket = inSocket;
	zmqItem.fd = 0;
	zmqItem.revents = 0;
	zmqItem.events = ZMQ_POLLIN;

	while (!finish)
	{
		// Poll for items with fixed timeout
		// TODO: change for C++ API once cppzmq supports it
		zmq_poll(&zmqItem, 1, pollTimeoutMillis);
		if (!(zmqItem.revents & ZMQ_POLLIN))
			continue;

		// Forward notifications
		zmq::message_t message;
		if (inSocket.recv(message))
		{
			outSocket.send(message, zmq::send_flags::dontwait);
		}
	}
}

void Client::Shutdown()
{
	finish = true;

	requestsThread.join();
	responsesThread.join();
	notificationsThread.join();
	signalsThread.join();
}

const uint64_t Client::SendNotification(NotifyMessage&& message)
{
	return SendNotification(std::move(message), GetNewNotificationId());
}

const uint64_t Client::SendNotification(NotifyMessage&& message, uint64_t id)
{
	// Thread static socket
	thread_local zmq::socket_t socket(context, zmq::socket_type::push);
	thread_local bool connected;

	// Ensure local (in-process) connection is established
	if (!connected)
	{
		socket.connect(notificationsInternalEndpoint);
		connected = true;
	}

	message.set_notificationid(id);

	// Serialize message
	const auto cbMessage = message.ByteSizeLong();
	std::vector<char> buffer(cbMessage);
	message.SerializeToArray(buffer.data(), cbMessage);

	// Send response to responses worker
	zmq::message_t msg(buffer.data(), cbMessage);
	socket.send(std::move(msg), zmq::send_flags::dontwait);
	return id;
}

void Client::SendResponse(SharpDetect::Common::Messages::NotifyMessage&& response, uint64_t requestId)
{
	// Thread static socket
	thread_local zmq::socket_t socket(context, zmq::socket_type::push);
	thread_local bool connected;

	// Ensure local (in-process) connection is established
	if (!connected)
	{
		socket.connect(responsesInternalEndpoint);
		connected = true;
	}

	// Serialize message
	const auto cbResponse = response.ByteSizeLong();
	std::vector<char> buffer(cbResponse);
	response.SerializeToArray(buffer.data(), cbResponse);

	// Send response to responses worker
	zmq::message_t msg(buffer.data(), cbResponse);
	socket.send(std::move(msg), zmq::send_flags::dontwait);
}

std::future<RequestMessage> Client::ReceiveRequest(uint64_t notificationId)
{
	auto lock = std::unique_lock<std::mutex>(promisesMutex);
	promises.insert(std::make_pair(notificationId, std::promise<RequestMessage>()));

	return promises[notificationId].get_future();
}
