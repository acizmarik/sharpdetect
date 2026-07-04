// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "RewriteRegistry.h"

void Profiler::RewriteRegistry::AddStub(const ModuleID moduleId, const mdMethodDef methodDef)
{
	auto guard = std::unique_lock(_methodStubsMutex);
	_methodStubs.emplace(std::make_pair(moduleId, methodDef), true);
}

BOOL Profiler::RewriteRegistry::IsStub(const ModuleID moduleId, const mdMethodDef methodDef)
{
	auto guard = std::shared_lock(_methodStubsMutex);
	return _methodStubs.contains(std::make_pair(moduleId, methodDef));
}

void Profiler::RewriteRegistry::AddModuleRewritings(const ModuleID moduleId, std::unordered_map<mdToken, mdToken> rewritings)
{
	auto guard = std::unique_lock(_rewritingsMutex);
	_rewritings.emplace(moduleId, std::move(rewritings));
}

void Profiler::RewriteRegistry::AddRewriting(const ModuleID moduleId, const mdToken fromToken, const mdToken toToken)
{
	auto guard = std::unique_lock(_rewritingsMutex);
	_rewritings.at(moduleId).emplace(fromToken, toToken);
}

std::unordered_map<mdToken, mdToken> Profiler::RewriteRegistry::GetModuleRewritings(const ModuleID moduleId)
{
	auto guard = std::unique_lock(_rewritingsMutex);
	const auto it = _rewritings.find(moduleId);
	return (it != _rewritings.cend()) ? it->second : std::unordered_map<mdToken, mdToken>{};
}

void Profiler::RewriteRegistry::AddModuleInjectedMethods(const ModuleID moduleId, std::unordered_map<LibIPC::RecordedEventType, mdToken> injectedMethods)
{
	auto guard = std::unique_lock(_injectedMethodsMutex);
	_injectedMethods.emplace(moduleId, std::move(injectedMethods));
}

Profiler::RewriteRegistry::ModulePatchData Profiler::RewriteRegistry::GetModulePatchData(const ModuleID moduleId)
{
	auto guardRewritings = std::unique_lock(_rewritingsMutex);
	auto guardInjections = std::unique_lock(_injectedMethodsMutex);

	const auto rewritingsIt = _rewritings.find(moduleId);
	const auto injectedIt = _injectedMethods.find(moduleId);
	const auto hasRewritings = rewritingsIt != _rewritings.cend();
	const auto hasInjected = injectedIt != _injectedMethods.cend();

	ModulePatchData data{};
	data.hasAny = hasRewritings || hasInjected;
	if (hasRewritings)
		data.tokensToRewrite = rewritingsIt->second;
	if (hasInjected)
		data.injectedMethods = injectedIt->second;

	return data;
}

void Profiler::RewriteRegistry::AddCustomEventMapping(
	CustomEventsLookup& lookup,
	const ModuleID moduleId,
	const mdMethodDef methodDef,
	const USHORT original,
	const USHORT mapping)
{
	if (static_cast<LibIPC::RecordedEventType>(mapping) == LibIPC::RecordedEventType::NotSpecified)
		return;

	auto guard = std::unique_lock(_customEventLookupsMutex);
	lookup.emplace(std::make_tuple(moduleId, methodDef, original), mapping);
}

void Profiler::RewriteRegistry::AddMethodEnterMapping(const ModuleID moduleId, const mdMethodDef methodDef, const USHORT original, const USHORT mapping)
{
	AddCustomEventMapping(_customEventOnMethodEntryLookup, moduleId, methodDef, original, mapping);
}

void Profiler::RewriteRegistry::AddMethodExitMapping(const ModuleID moduleId, const mdMethodDef methodDef, const USHORT original, const USHORT mapping)
{
	AddCustomEventMapping(_customEventOnMethodExitLookup, moduleId, methodDef, original, mapping);
}

Profiler::RewriteRegistry::CustomEventMappingResult Profiler::RewriteRegistry::FindCustomEventMappings(
	const CustomEventsLookup& lookup,
	const ModuleID moduleId,
	const mdMethodDef methodDef,
	const USHORT originalEvent,
	const USHORT originalWithArgsEvent)
{
	CustomEventMappingResult result{};
	auto guard = std::shared_lock(_customEventLookupsMutex);

	auto it = lookup.find(std::make_tuple(moduleId, methodDef, originalEvent));
	result.hasEvent = (it != lookup.cend());
	if (result.hasEvent)
		result.eventMapping = it->second;

	it = lookup.find(std::make_tuple(moduleId, methodDef, originalWithArgsEvent));
	result.hasWithArgsEvent = (it != lookup.cend());
	if (result.hasWithArgsEvent)
		result.withArgsEventMapping = it->second;

	return result;
}

Profiler::RewriteRegistry::CustomEventMappingResult Profiler::RewriteRegistry::FindMethodEnterMappings(
	const ModuleID moduleId,
	const mdMethodDef methodDef,
	const USHORT originalEvent,
	const USHORT originalWithArgsEvent)
{
	return FindCustomEventMappings(_customEventOnMethodEntryLookup, moduleId, methodDef, originalEvent, originalWithArgsEvent);
}

Profiler::RewriteRegistry::CustomEventMappingResult Profiler::RewriteRegistry::FindMethodExitMappings(
	const ModuleID moduleId,
	const mdMethodDef methodDef,
	const USHORT originalEvent,
	const USHORT originalWithArgsEvent)
{
	return FindCustomEventMappings(_customEventOnMethodExitLookup, moduleId, methodDef, originalEvent, originalWithArgsEvent);
}
