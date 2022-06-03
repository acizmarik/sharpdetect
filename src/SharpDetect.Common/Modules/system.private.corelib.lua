-- This file describes System.Private.CoreLib
local sd = require("sharpdetect")

assemblyName = "System.Private.CoreLib"
isCoreLibrary = true

function getMethodDescriptors(list)
	getMonitorLockMethods(list)
	getMonitorSignalMethods(list)
	getInjectedHelpers(list)
end

-- Get descriptors necessary to capture all calls to Monitor::Enter(...) and Monitor::Exit(...)
function getMonitorLockMethods(list)
	declaringType = "System.Threading.Monitor"
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

function getMonitorSignalMethods(list)

end

function getInjectedHelpers(list)

end