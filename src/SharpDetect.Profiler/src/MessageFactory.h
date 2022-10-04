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

#ifndef MESSAGEFACTORY_HEADER_GUARD
#define MESSAGEFACTORY_HEADER_GUARD

#include "cor.h"
#include "corprof.h"
#include "profiler_notifications.pb.h"
#include "profiler_requests.pb.h"
#include "wstring.h"

class MessageFactory
{
public:
	static SharpDetect::Common::Messages::NotifyMessage Heartbeat();
	static SharpDetect::Common::Messages::NotifyMessage RequestProcessed(ThreadID thread, ULONG requestId, bool result);
	static SharpDetect::Common::Messages::NotifyMessage ProfilerInitialized(ThreadID thread);
	static SharpDetect::Common::Messages::NotifyMessage ProfilerDestroyed(ThreadID thread);
	static SharpDetect::Common::Messages::NotifyMessage ModuleLoaded(ThreadID thread, ModuleID moduleId, const WSTRING& path);
	static SharpDetect::Common::Messages::NotifyMessage TypeLoaded(ThreadID thread, ModuleID moduleId, mdTypeDef classToken);
	static SharpDetect::Common::Messages::NotifyMessage JITCompilationStarted(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken);
	static SharpDetect::Common::Messages::NotifyMessage ThreadCreated(ThreadID thread, ThreadID newThreadId);
	static SharpDetect::Common::Messages::NotifyMessage ThreadDestroyed(ThreadID thread, ThreadID newThreadId);
	static SharpDetect::Common::Messages::NotifyMessage TypeInjected(ThreadID thread, ModuleID moduleId, mdTypeDef classToken);
	static SharpDetect::Common::Messages::NotifyMessage TypeReferenced(ThreadID thread, ModuleID moduleId, mdTypeRef classRefToken);
	static SharpDetect::Common::Messages::NotifyMessage MethodInjected(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, SharpDetect::Common::Messages::MethodType type);
	static SharpDetect::Common::Messages::NotifyMessage HelperMethodReferenced(ThreadID thread, ModuleID moduleId, mdTypeRef classRefToken, mdMemberRef functionRefToken, SharpDetect::Common::Messages::MethodType type);
	static SharpDetect::Common::Messages::NotifyMessage WrapperMethodReferenced(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, ModuleID refModuleId, mdTypeRef classRefToken, mdMemberRef functionRefToken);
	static SharpDetect::Common::Messages::NotifyMessage MethodWrapped(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef nativeFunctionToken, mdMethodDef wrapperFunctionToken);
	static SharpDetect::Common::Messages::NotifyMessage MethodCalled(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken);
	static SharpDetect::Common::Messages::NotifyMessage MethodCalledWithArguments(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, BYTE* argValues, ULONG cbArgValues, BYTE* argInfos, ULONG cbArgInfos);
	static SharpDetect::Common::Messages::NotifyMessage MethodReturned(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken);
	static SharpDetect::Common::Messages::NotifyMessage MethodReturnedWithReturnValue(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, BYTE* returnValue, ULONG cbReturnValue, BYTE* argValues, ULONG cbArgValues, BYTE* argInfos, ULONG cbArgInfos);
	static SharpDetect::Common::Messages::NotifyMessage GarbageCollectionStarted(ThreadID thread, BYTE* generations, ULONG cbGenerations, BYTE* bounds, ULONG cbBounds);
	static SharpDetect::Common::Messages::NotifyMessage GarbageCollectionFinished(ThreadID thread, BYTE* bounds, ULONG cbBounds);
	static SharpDetect::Common::Messages::NotifyMessage RuntimeSuspendStarted(ThreadID thread, COR_PRF_SUSPEND_REASON reason);
	static SharpDetect::Common::Messages::NotifyMessage RuntimeSuspendFinished(ThreadID thread);
	static SharpDetect::Common::Messages::NotifyMessage RuntimeResumeStarted(ThreadID thread);
	static SharpDetect::Common::Messages::NotifyMessage RuntimeResumeFinished(ThreadID thread);
	static SharpDetect::Common::Messages::NotifyMessage RuntimeThreadSuspended(ThreadID thread, ThreadID suspendedThread);
	static SharpDetect::Common::Messages::NotifyMessage RuntimeThreadResumed(ThreadID thread, ThreadID resumedThread);
	static SharpDetect::Common::Messages::NotifyMessage SurvivingReferences(ThreadID thread, BYTE* ranges, ULONG cbRanges, BYTE* lengths, ULONG cbLengths);
	static SharpDetect::Common::Messages::NotifyMessage MovedReferences(ThreadID thread, BYTE* oldRanges, ULONG cbOldRanges, BYTE* newRanges, ULONG cbNewRanges, BYTE* lengths, ULONG cbLengths);
};

#endif