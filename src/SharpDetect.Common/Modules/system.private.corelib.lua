-- This file describes System.Private.CoreLib
local sd = require("Imports/sharpdetect")

assemblyName = "System.Private.CoreLib"
isCoreLibrary = true

function getMethodDescriptors(list)
	getMonitorLockMethods(list)
	getMonitorSignalMethods(list)
	getInjectedHelpers(list)
end

-- Get descriptors necessary to capture all calls to Monitor::Enter(...) and Monitor::Exit(...)
function getMonitorLockMethods(list)
	local declaringType = "System.Threading.Monitor"
	-- System.Void System.Threading.Monitor::Enter(System.Object)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Enter", declaringType, true, 1, { "System.Object" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.LockBlockingAcquire, 
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.InjectManagedWrapper },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadConstant(true, "System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Void System.Threading.Monitor::Enter(System.Object,System.Boolean&)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Enter", declaringType, true, 2, { "System.Object", "System.Boolean&" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.LockBlockingAcquire,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.InjectManagedWrapper },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false),
					sd.createCapturedParameterInfo(1, 1, true)
				},
				sd.createExpressionBuilder()
					.LoadArgument(0)
					.Member("Item2")
					.Member("BoxedValue")
					.Unbox("System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Void System.Threading.Monitor::ReliableEnterTimeout(System.Object,System.Int32,System.Boolean&)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("ReliableEnterTimeout", declaringType, true, 3, { "System.Object", "System.Int32", "System.Boolean&" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.LockTryAcquire,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.InjectManagedWrapper },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false),
					sd.createCapturedParameterInfo(1, 4, false),
					sd.createCapturedParameterInfo(2, 1, true)
				},
				sd.createExpressionBuilder()
					.LoadArgument(0)
					.Member("Item2")
					.Member("BoxedValue")
					.Unbox("System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Void System.Threading.Monitor::Exit(System.Object)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Exit", declaringType, true, 1, { "System.Object" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.LockRelease,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.InjectManagedWrapper },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadConstant(true, "System.Boolean")
					.Compile()
			)
		)
	)
end

-- Get description necessary to capture all calls to Monitor::Wait(...), Monitor::Pulse(...) and Monitor::PulseAll(...)
function getMonitorSignalMethods(list)
	local declaringType = "System.Threading.Monitor"
	-- System.Boolean System.Threading.Monitor::Wait(System.Object)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Wait", declaringType, true, 1, { "System.Object" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.SignalBlockingWait,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.CaptureReturnValue },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadReturnValue()
					.Member("BoxedValue")
					.Unbox("System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Boolean System.Threading.Monitor::Wait(System.Object,System.TimeSpan)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Wait", declaringType, true, 2, { "System.Object", "System.TimeSpan" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.SignalTryWait,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.CaptureReturnValue },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadReturnValue()
					.Member("BoxedValue")
					.Unbox("System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Boolean System.Threading.Monitor::Wait(System.Object,System.Int32)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Wait", declaringType, true, 2, { "System.Object", "System.Int32" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.SignalTryWait,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.CaptureReturnValue },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadReturnValue()
					.Member("BoxedValue")
					.Unbox("System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Boolean System.Threading.Monitor::Wait(System.Object,System.Int32,System.Boolean)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Wait", declaringType, true, 3, { "System.Object", "System.Int32", "System.Boolean" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.SignalTryWait,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments, MethodRewritingFlags.CaptureReturnValue },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadReturnValue()
					.Member("BoxedValue")
					.Unbox("System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Void System.Threading.Monitor::Pulse(System.Object)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("Pulse", declaringType, true, 1, { "System.Object" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.SignalPulseOne,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadConstant(true, "System.Boolean")
					.Compile()
			)
		)
	)
	-- System.Void System.Threading.Monitor::PulseAll(System.Object)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("PulseAll", declaringType, true, 1, { "System.Object" }, false),
			sd.createMethodInterpretation(
				MethodInterpretation.SignalPulseAll,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments },
				{
					sd.createCapturedParameterInfo(0, UIntPtr.Size, false)
				},
				sd.createExpressionBuilder()
					.LoadConstant(true, "System.Boolean")
					.Compile()
			)
		)
	)
end

-- Get descriptors necessary to capture all injected helper methods
function getInjectedHelpers(list)
	local declaringType = "SharpDetect.EventDispatcher"
	-- System.Void System.SharpDetect::FieldAccess(System.Boolean,System.UInt64)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("FieldAccess", declaringType, true, 2, { "System.Boolean", "System.UInt64" }, true),
			sd.createMethodInterpretation(
				MethodInterpretation.FieldAccess,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments },
				{
					sd.createCapturedParameterInfo(0, 1, false),
					sd.createCapturedParameterInfo(1, 8, false)
				}, nil
			)
		)
	)
	-- System.Void System.SharpDetect::FieldInstanceAccess(System.Object)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("FieldInstanceAccess", declaringType, true, 1, { "System.Object" }, true),
			sd.createMethodInterpretation(
				MethodInterpretation.FieldInstanceAccess,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments },
				{
					sd.createCapturedParameterInfo(0, 8, false)
				}, nil
			)
		)
	)
	-- System.Void System.SharpDetect::ArrayElementAccess(System.Boolean,System.UInt64)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("ArrayElementAccess", declaringType, true, 2, { "System.Boolean", "System.UInt64" }, true),
			sd.createMethodInterpretation(
				MethodInterpretation.ArrayElementAccess,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments },
				{
					sd.createCapturedParameterInfo(0, 1, false),
					sd.createCapturedParameterInfo(1, 8, false)
				}, nil
			)
		)
	)
	-- System.Void System.SharpDetect::ArrayInstanceAccess(System.Object)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("ArrayInstanceAccess", declaringType, true, 1, { "System.Object" }, true),
			sd.createMethodInterpretation(
				MethodInterpretation.ArrayInstanceAccess,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments },
				{
					sd.createCapturedParameterInfo(0, 8, false)
				}, nil
			)
		)
	)
	-- System.Void System.SharpDetect::ArrayIndexAccess(System.Int32)
	list.add(
		sd.createMethodRecord(
			sd.createMethodIdentifier("ArrayIndexAccess", declaringType, true, 1, { "System.Int32" }, true),
			sd.createMethodInterpretation(
				MethodInterpretation.ArrayIndexAccess,
				sd.flagsOr{ MethodRewritingFlags.InjectEntryExitHooks, MethodRewritingFlags.CaptureArguments },
				{
					sd.createCapturedParameterInfo(0, 4, false)
				}, nil
			)
		)
	)
end