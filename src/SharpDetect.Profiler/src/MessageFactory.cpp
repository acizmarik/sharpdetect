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
#include "MessageFactory.h"
#include "PAL.h"

using namespace SharpDetect::Common::Messages;

SharpDetect::Common::Messages::NotifyMessage MessageFactory::RequestProcessed(ThreadID thread, ULONG requestId, bool result)
{
	auto message = NotifyMessage();	
	auto response = new Response();
	response->set_requestid(requestId);
	response->set_result(result);
	message.set_allocated_response(response);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

NotifyMessage MessageFactory::ProfilerInitialized(ThreadID thread)
{
	auto message = NotifyMessage();
	auto profilerInitialized = new Notify_ProfilerInitialized();
	message.set_allocated_profilerinitialized(profilerInitialized);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::ProfilerDestroyed(ThreadID thread)
{
	auto message = NotifyMessage();
	auto profilerDestroyed = new Notify_ProfilerDestroyed();
	message.set_allocated_profilerdestroyed(profilerDestroyed);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::ModuleLoaded(ThreadID thread, ModuleID moduleId, const WSTRING& modulePath)
{
	auto message = NotifyMessage();
	auto moduleLoadedMessage = new Notify_ModuleLoaded();
	moduleLoadedMessage->set_moduleid(moduleId);
	moduleLoadedMessage->set_modulepath(ToString(modulePath));
	message.set_allocated_moduleloaded(moduleLoadedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::TypeLoaded(ThreadID thread, ModuleID moduleId, mdTypeDef classToken)
{
	auto message = NotifyMessage();
	auto classLoadedMessage = new Notify_TypeLoaded();
	classLoadedMessage->set_moduleid(moduleId);
	classLoadedMessage->set_typetoken(classToken);
	message.set_allocated_typeloaded(classLoadedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::JITCompilationStarted(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken)
{
	auto message = NotifyMessage();
	auto functionCompilationStartedMessage = new Notify_JITCompilationStarted();
	functionCompilationStartedMessage->set_moduleid(moduleId);
	functionCompilationStartedMessage->set_typetoken(classToken);
	functionCompilationStartedMessage->set_functiontoken(functionToken);
	message.set_allocated_jitcompilationstarted(functionCompilationStartedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::ThreadCreated(ThreadID thread, ThreadID newThreadId)
{
	auto message = NotifyMessage();
	auto threadCreatedMessage = new Notify_ThreadCreated();
	threadCreatedMessage->set_threadid(newThreadId);
	message.set_allocated_threadcreated(threadCreatedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::ThreadDestroyed(ThreadID thread, ThreadID newThreadId)
{
	auto message = NotifyMessage();
	auto threadCreatedMessage = new Notify_ThreadDestroyed();
	threadCreatedMessage->set_threadid(newThreadId);
	message.set_allocated_threaddestroyed(threadCreatedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::TypeInjected(ThreadID thread, ModuleID moduleId, mdTypeDef classToken)
{
	auto message = NotifyMessage();
	auto typeInjectedMessage = new Notify_TypeInjected();
	typeInjectedMessage->set_moduleid(moduleId);
	typeInjectedMessage->set_typetoken(classToken);
	message.set_allocated_typeinjected(typeInjectedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::TypeReferenced(ThreadID thread, ModuleID moduleId, mdTypeRef classRefToken)
{
	auto message = NotifyMessage();
	auto typeReferencedMessage = new Notify_TypeReferenced();
	typeReferencedMessage->set_moduleid(moduleId);
	typeReferencedMessage->set_typetoken(classRefToken);
	message.set_allocated_typereferenced(typeReferencedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::MethodInjected(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, MethodType type)
{
	auto message = NotifyMessage();
	auto methodInjectedMessage = new Notify_MethodInjected();
	methodInjectedMessage->set_moduleid(moduleId);
	methodInjectedMessage->set_typetoken(classToken);
	methodInjectedMessage->set_functiontoken(functionToken);
	methodInjectedMessage->set_type(type);
	message.set_allocated_methodinjected(methodInjectedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::HelperMethodReferenced(ThreadID thread, ModuleID moduleId, mdTypeRef classRefToken, mdMemberRef functionRefToken, MethodType type)
{
	auto message = NotifyMessage();
	auto helperMethodReferencedMessage = new Notify_HelperMethodReferenced();
	helperMethodReferencedMessage->set_moduleid(moduleId);
	helperMethodReferencedMessage->set_typetoken(classRefToken);
	helperMethodReferencedMessage->set_functiontoken(functionRefToken);
	helperMethodReferencedMessage->set_type(type);
	message.set_allocated_helpermethodreferenced(helperMethodReferencedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::WrapperMethodReferenced(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, ModuleID refModuleId, mdTypeRef classRefToken, mdMemberRef functionRefToken)
{
	auto message = NotifyMessage();
	auto wrapperMethodReferencedMessage = new Notify_WrapperMethodReferenced();
	wrapperMethodReferencedMessage->set_defmoduleid(moduleId);
	wrapperMethodReferencedMessage->set_deftypetoken(classToken);
	wrapperMethodReferencedMessage->set_deffunctiontoken(functionToken);
	wrapperMethodReferencedMessage->set_refmoduleid(refModuleId);
	wrapperMethodReferencedMessage->set_reftypetoken(classRefToken);
	wrapperMethodReferencedMessage->set_reffunctiontoken(functionRefToken);
	message.set_allocated_wrappermethodreferenced(wrapperMethodReferencedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::MethodWrapped(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef nativeFunctionToken, mdMethodDef wrapperFunctionToken)
{
	auto message = NotifyMessage();
	auto methodWrappedMessage = new Notify_MethodWrapperInjected();
	methodWrappedMessage->set_moduleid(moduleId);
	methodWrappedMessage->set_typetoken(classToken);
	methodWrappedMessage->set_originalfunctiontoken(nativeFunctionToken);
	methodWrappedMessage->set_wrapperfunctiontoken(wrapperFunctionToken);
	message.set_allocated_methodwrapperinjected(methodWrappedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::MethodCalled(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken)
{
	auto message = NotifyMessage();
	auto methodCalledMessage = new Notify_MethodCalled();
	methodCalledMessage->set_moduleid(moduleId);
	methodCalledMessage->set_typetoken(classToken);
	methodCalledMessage->set_functiontoken(functionToken);
	message.set_allocated_methodcalled(methodCalledMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::MethodCalledWithArguments(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, BYTE* argValues, ULONG cbArgValues, BYTE* argInfos, ULONG cbArgInfos)
{
	auto message = NotifyMessage();
	auto methodCalledMessage = new Notify_MethodCalled();
	methodCalledMessage->set_moduleid(moduleId);
	methodCalledMessage->set_typetoken(classToken);
	methodCalledMessage->set_functiontoken(functionToken);
	methodCalledMessage->set_argumentvalues(ToString(argValues, cbArgValues));
	methodCalledMessage->set_argumentoffsets(ToString(argInfos, cbArgInfos));
	message.set_allocated_methodcalled(methodCalledMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::MethodReturned(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken)
{
	auto message = NotifyMessage();
	auto methodReturnedMessage = new Notify_MethodReturned();
	methodReturnedMessage->set_moduleid(moduleId);
	methodReturnedMessage->set_typetoken(classToken);
	methodReturnedMessage->set_functiontoken(functionToken);
	message.set_allocated_methodreturned(methodReturnedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::MethodReturnedWithReturnValue(ThreadID thread, ModuleID moduleId, mdTypeDef classToken, mdMethodDef functionToken, 
	BYTE* returnValue, ULONG cbReturnValue, BYTE* argValues, ULONG cbArgValues, BYTE* argInfos, ULONG cbArgInfos)
{
	auto message = NotifyMessage();
	auto methodReturnedMessage = new Notify_MethodReturned();
	methodReturnedMessage->set_moduleid(moduleId);
	methodReturnedMessage->set_typetoken(classToken);
	methodReturnedMessage->set_functiontoken(functionToken);
	methodReturnedMessage->set_returnvalue(ToString(returnValue, cbReturnValue));
	methodReturnedMessage->set_byrefargumentvalues(ToString(argValues, cbArgValues));
	methodReturnedMessage->set_byrefargumentoffsets(ToString(argInfos, cbArgInfos));
	message.set_allocated_methodreturned(methodReturnedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::GarbageCollectionStarted(ThreadID thread, BYTE* generations, ULONG cbGenerations, BYTE* bounds, ULONG cbBounds)
{
	auto message = NotifyMessage();
	auto gcStartedMessage = new Notify_GarbageCollectionStarted();
	gcStartedMessage->set_generationscollected(ToString(generations, cbGenerations));
	gcStartedMessage->set_generationsegmentbounds(ToString(bounds, cbBounds));
	message.set_allocated_garbagecollectionstarted(gcStartedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::GarbageCollectionFinished(ThreadID thread, BYTE* bounds, ULONG cbBounds)
{
	auto message = NotifyMessage();
	auto gcFinishedMessage = new Notify_GarbageCollectionFinished();
	gcFinishedMessage->set_generationsegmentbounds(ToString(bounds, cbBounds));
	message.set_allocated_garbagecollectionfinished(gcFinishedMessage);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::RuntimeSuspendStarted(ThreadID thread, COR_PRF_SUSPEND_REASON reason)
{
	auto message = NotifyMessage();
	auto runtimeSuspendStarted = new Notify_RuntimeSuspendStarted();
	auto suspensionReason = static_cast<SharpDetect::Common::Messages::SUSPEND_REASON>(reason);
	runtimeSuspendStarted->set_reason(suspensionReason);
	message.set_allocated_runtimesuspendstarted(runtimeSuspendStarted);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::RuntimeSuspendFinished(ThreadID thread)
{
	auto message = NotifyMessage();
	auto runtimeSuspendFinished = new Notify_RuntimeSuspendFinished();
	message.set_allocated_runtimesuspendfinished(runtimeSuspendFinished);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::RuntimeThreadSuspended(ThreadID thread, ThreadID suspendedThread)
{
	auto message = NotifyMessage();
	auto runtimeThreadSuspended = new Notify_RuntimeThreadSuspended();
	runtimeThreadSuspended->set_threadid(suspendedThread);
	message.set_allocated_runtimethreadsuspended(runtimeThreadSuspended);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::RuntimeThreadResumed(ThreadID thread, ThreadID resumedThread)
{
	auto message = NotifyMessage();
	auto runtimeThreadResumed = new Notify_RuntimeThreadResumed();
	runtimeThreadResumed->set_threadid(resumedThread);
	message.set_allocated_runtimethreadresumed(runtimeThreadResumed);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::SurvivingReferences(ThreadID thread, BYTE* ranges, ULONG cbRanges, BYTE* lengths, ULONG cbLengths)
{
	auto message = NotifyMessage();
	auto survivingReferences = new Notify_SurvivingReferences();
	survivingReferences->set_blocks(ToString(ranges, cbRanges));
	survivingReferences->set_lengths(ToString(lengths, cbLengths));
	message.set_allocated_survivingreferences(survivingReferences);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}

SharpDetect::Common::Messages::NotifyMessage MessageFactory::MovedReferences(ThreadID thread, BYTE* oldRanges, ULONG cbOldRanges, BYTE* newRanges, ULONG cbNewRanges, BYTE* lengths, ULONG cbLengths)
{
	auto message = NotifyMessage();
	auto movedReferences = new Notify_MovedReferences();
	movedReferences->set_oldblocks(ToString(oldRanges, cbOldRanges));
	movedReferences->set_newblocks(ToString(newRanges, cbNewRanges));
	movedReferences->set_lengths(ToString(lengths, cbLengths));
	message.set_allocated_movedreferences(movedReferences);
	message.set_threadid(thread);
	message.set_processid(PAL::GetProcessId());
	return message;
}
