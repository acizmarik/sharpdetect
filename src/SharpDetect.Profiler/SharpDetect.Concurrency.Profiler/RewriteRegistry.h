// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <mutex>
#include <shared_mutex>
#include <tuple>
#include <unordered_map>
#include <utility>

#include "cor.h"
#include "corprof.h"

#include "../LibIPC/Messages.h"
#include "../LibDescriptors/HashingUtils.h"

namespace Profiler
{
	class RewriteRegistry
	{
	public:
		void AddStub(ModuleID moduleId, mdMethodDef methodDef);
		[[nodiscard]] BOOL IsStub(ModuleID moduleId, mdMethodDef methodDef);

		void AddModuleRewritings(ModuleID moduleId, std::unordered_map<mdToken, mdToken> rewritings);
		void AddRewriting(ModuleID moduleId, mdToken fromToken, mdToken toToken);
		[[nodiscard]] std::unordered_map<mdToken, mdToken> GetModuleRewritings(ModuleID moduleId);

		void AddModuleInjectedMethods(ModuleID moduleId, std::unordered_map<LibIPC::RecordedEventType, mdToken> injectedMethods);
		
		struct ModulePatchData
		{
			BOOL hasAny;
			std::unordered_map<mdToken, mdToken> tokensToRewrite;
			std::unordered_map<LibIPC::RecordedEventType, mdToken> injectedMethods;
		};
		[[nodiscard]] ModulePatchData GetModulePatchData(ModuleID moduleId);

		struct CustomEventMappingResult
		{
			BOOL hasEvent;
			USHORT eventMapping;
			BOOL hasWithArgsEvent;
			USHORT withArgsEventMapping;
		};

		void AddMethodEnterMapping(ModuleID moduleId, mdMethodDef methodDef, USHORT original, USHORT mapping);
		void AddMethodExitMapping(ModuleID moduleId, mdMethodDef methodDef, USHORT original, USHORT mapping);
		[[nodiscard]] CustomEventMappingResult FindMethodEnterMappings(ModuleID moduleId, mdMethodDef methodDef, USHORT originalEvent, USHORT originalWithArgsEvent);
		[[nodiscard]] CustomEventMappingResult FindMethodExitMappings(ModuleID moduleId, mdMethodDef methodDef, USHORT originalEvent, USHORT originalWithArgsEvent);

	private:
		using MethodId = std::pair<ModuleID, mdMethodDef>;
		using MethodIdHasher = pair_hash<ModuleID, mdMethodDef>;
		using MethodInvocationId = std::tuple<ModuleID, mdMethodDef, USHORT>;
		using MethodInvocationIdHasher = tuple_hash<ModuleID, mdMethodDef, USHORT>;
		using CustomEventsLookup = std::unordered_map<MethodInvocationId, USHORT, MethodInvocationIdHasher>;

		void AddCustomEventMapping(CustomEventsLookup& lookup, ModuleID moduleId, mdMethodDef methodDef, USHORT original, USHORT mapping);
		CustomEventMappingResult FindCustomEventMappings(const CustomEventsLookup& lookup, ModuleID moduleId, mdMethodDef methodDef, USHORT originalEvent, USHORT originalWithArgsEvent);

		std::unordered_map<ModuleID, std::unordered_map<mdToken, mdToken>> _rewritings;
		std::mutex _rewritingsMutex;

		std::unordered_map<ModuleID, std::unordered_map<LibIPC::RecordedEventType, mdToken>> _injectedMethods;
		std::mutex _injectedMethodsMutex;

		std::unordered_map<MethodId, BOOL, MethodIdHasher> _methodStubs;
		std::mutex _methodStubsMutex;

		CustomEventsLookup _customEventOnMethodEntryLookup;
		CustomEventsLookup _customEventOnMethodExitLookup;
		std::shared_mutex _customEventLookupsMutex;
	};
}
