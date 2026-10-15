// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"
#include "corprof.h"

namespace Profiler
{
	struct FieldAccessHelperInfo
	{
		ModuleID moduleId;
		mdMethodDef methodDef;
		USHORT interpretation;
		bool isInstance;
		bool captureStackTrace;
	};
}

// Called by injected field access helpers through calli unmanaged[SuppressGCTransition]
// The instance is passed as the address of the helper's argument slot (null for static fields)
// The slot is reported to the GC, so it holds the current reference. We stays in cooperative mode to translate it
EXTERN_C void STDMETHODCALLTYPE FieldAccessStub(const Profiler::FieldAccessHelperInfo* helper, UINT64 instrumentationMark, const ObjectID* instanceSlot);

// Called by stack-capturing helpers through calli unmanaged before FieldAccessStub. The full managed->unmanaged
// transition gives DoStackSnapshot a frame to start from
EXTERN_C void STDMETHODCALLTYPE FieldAccessStackStub();
