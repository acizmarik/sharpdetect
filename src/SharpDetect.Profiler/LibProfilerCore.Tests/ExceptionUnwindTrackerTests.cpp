// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <vector>

#include "doctest.h"

#include "ExceptionUnwindTracker.h"

using LibProfiler::ExceptionUnwindTracker;

namespace
{
	constexpr FunctionID Thrower = 1;
	constexpr FunctionID Method = 2;
	constexpr FunctionID Caller = 3;
	constexpr FunctionID Outer = 4;
	constexpr FunctionID Filter = 5;

	struct Replay
	{
		ExceptionUnwindTracker tracker;
		std::vector<FunctionID> left;

		Replay& Enter(const FunctionID functionId) { tracker.OnUnwindFunctionEnter(functionId); return *this; }
		Replay& Finally(const FunctionID functionId) { tracker.OnUnwindFinallyEnter(functionId); return *this; }
		Replay& FinallyLeave() { tracker.OnUnwindFinallyLeave(); return *this; }
		Replay& Catch(const FunctionID functionId) { tracker.OnCatcherEnter(functionId); return *this; }
		Replay& FilterEnter(const FunctionID functionId) { tracker.OnSearchFilterEnter(functionId); return *this; }
		Replay& FilterLeave() { tracker.OnSearchFilterLeave(); return *this; }

		Replay& Throw()
		{
			if (const auto leftFunctionId = tracker.OnExceptionThrown())
				left.push_back(*leftFunctionId);
			return *this;
		}

		Replay& Leave()
		{
			if (const auto leftFunctionId = tracker.OnUnwindFunctionLeave())
				left.push_back(*leftFunctionId);
			return *this;
		}
	};
}

TEST_CASE("ExceptionUnwindTracker reports a rethrowing frame once")
{
	// Outer() { try { Caller(); } catch { } } Caller() { Method(); } Method() { try { Thrower(); } catch { throw; } }
	Replay replay;
	replay.Throw()
		.Enter(Thrower).Leave()
		.Enter(Method).Catch(Method)
		.Throw()
		.Enter(Method).Leave()
		.Enter(Caller).Leave()
		.Enter(Outer).Catch(Outer);

	CHECK(replay.left == std::vector<FunctionID> { Thrower, Method, Caller });
	CHECK(replay.tracker.GetPendingFramesCount() == 0);
}

TEST_CASE("ExceptionUnwindTracker keeps a frame whose finally handles another exception")
{
	// Outer() { try { Method(); } catch { } } Method() { try { Thrower(); } finally { try { Thrower(); } catch { } } }
	Replay replay;
	replay.Throw()
		.Enter(Thrower).Leave()
		.Enter(Method).Finally(Method)
		.Throw()
		.Enter(Thrower).Leave()
		.Enter(Method).Catch(Method)
		.FinallyLeave().Leave()
		.Enter(Outer).Catch(Outer);

	CHECK(replay.left == std::vector<FunctionID> { Thrower, Thrower, Method });
	CHECK(replay.tracker.GetPendingFramesCount() == 0);
}

TEST_CASE("ExceptionUnwindTracker reports an escaping finally nested in another finally")
{
	// Outer() { try { Caller(); } catch { } } Caller() { try { Thrower(); } finally { try { Method(); } catch { } } }
	// Method() { try { Thrower(); } finally { Thrower(); } }
	Replay replay;
	replay.Throw()
		.Enter(Thrower).Leave()
		.Enter(Caller).Finally(Caller)
		.Throw()
		.Enter(Thrower).Leave()
		.Enter(Method).Finally(Method)
		.Throw()
		.Enter(Thrower).Leave()
		.Enter(Method).Leave()
		.Enter(Caller).Catch(Caller)
		.FinallyLeave().Leave()
		.Enter(Outer).Catch(Outer);

	CHECK(replay.left == std::vector<FunctionID> { Thrower, Thrower, Thrower, Method, Caller });
	CHECK(replay.tracker.GetPendingFramesCount() == 0);
}

TEST_CASE("ExceptionUnwindTracker does not report the frame of a filter that an exception escapes (.NET 9+)")
{
	// Method() { try { try { Thrower(); } catch when (Filter()) { } } catch { } } Filter() { Thrower(); return true; }
	Replay replay;
	replay.Throw()
		.FilterEnter(Method)
		.Throw()
		.Leave()
		.Enter(Thrower).Leave()
		.Enter(Filter).Leave()
		.Enter(Method)
		.FilterLeave()
		.Enter(Thrower).Leave()
		.Enter(Method).Catch(Method);

	CHECK(replay.left == std::vector<FunctionID> { Thrower, Filter, Thrower });
	CHECK(replay.tracker.GetPendingFramesCount() == 0);
}

TEST_CASE("ExceptionUnwindTracker keeps a frame running a finally across its own filter that an exception escapes")
{
	// Outer() { try { Caller(); } catch { } } Caller() { Method(); } Filter() { Thrower(); return true; }
	// Method() { try { Thrower(); } finally { try { Thrower(); } catch when (Filter()) { } catch { } } }
	SUBCASE(".NET 8")
	{
		Replay replay;
		replay.Throw()
			.Enter(Thrower).Leave()
			.Enter(Method).Finally(Method)
			.Throw()
			.FilterEnter(Method)
			.Throw()
			.Enter(Thrower).Leave()
			.Enter(Filter).Leave()
			.Enter(Method).Leave()
			.FilterLeave()
			.Enter(Thrower).Leave()
			.Enter(Method).Catch(Method)
			.FinallyLeave().Leave()
			.Enter(Caller).Leave()
			.Enter(Outer).Catch(Outer);

		CHECK(replay.left == std::vector<FunctionID> { Thrower, Thrower, Filter, Thrower, Method, Caller });
		CHECK(replay.tracker.GetPendingFramesCount() == 0);
	}

	SUBCASE(".NET 9+")
	{
		Replay replay;
		replay.Throw()
			.Enter(Thrower).Leave()
			.Enter(Method).Finally(Method)
			.Throw()
			.FilterEnter(Method)
			.Throw()
			.Leave()
			.Enter(Thrower).Leave()
			.Enter(Filter).Leave()
			.Enter(Method)
			.FilterLeave()
			.Enter(Thrower).Leave()
			.Enter(Method).Catch(Method)
			.FinallyLeave().Leave()
			.Enter(Caller).Leave()
			.Enter(Outer).Catch(Outer);

		CHECK(replay.left == std::vector<FunctionID> { Thrower, Thrower, Filter, Thrower, Method, Caller });
		CHECK(replay.tracker.GetPendingFramesCount() == 0);
	}
}

// On .NET 8, the frame where an exception leaves managed code reports an ordinary leave, as in the tests above
TEST_CASE("ExceptionUnwindTracker reports the frame where an exception leaves managed code (.NET 9+)")
{
	// Caller() { try { _ = Method.Value; } catch (TypeInitializationException) { } }
	// Method { static readonly int Value = Thrower(); } - the runtime runs the type initializer from native code.
	Replay replay;
	replay.Throw()
		.Leave()
		.Enter(Thrower).Leave()
		.Enter(Method)
		.Throw()
		.Enter(Caller).Catch(Caller);

	CHECK(replay.left == std::vector<FunctionID> { Thrower, Method });
	CHECK(replay.tracker.GetPendingFramesCount() == 0);
}

TEST_CASE("ExceptionUnwindTracker reports the frame where an exception leaves managed code after its finally (.NET 9+)")
{
	// As above with Method { static Method() { try { Thrower(); } finally { } } }
	Replay replay;
	replay.Throw()
		.Leave()
		.Enter(Thrower).Leave()
		.Enter(Method).Finally(Method).FinallyLeave()
		.Throw()
		.Enter(Caller).Catch(Caller);

	CHECK(replay.left == std::vector<FunctionID> { Thrower, Method });
	CHECK(replay.tracker.GetPendingFramesCount() == 0);
}

TEST_CASE("ExceptionUnwindTracker reports a frame once when an exception escapes its finally into native code (.NET 9+)")
{
	// As above with Method { static Method() { try { Thrower(); } finally { Thrower(); } } }
	Replay replay;
	replay.Throw()
		.Leave()
		.Enter(Thrower).Leave()
		.Enter(Method).Finally(Method)
		.Throw()
		.Leave()
		.Enter(Thrower).Leave()
		.Enter(Method)
		.Throw()
		.Enter(Caller).Catch(Caller);

	CHECK(replay.left == std::vector<FunctionID> { Thrower, Thrower, Method });
	CHECK(replay.tracker.GetPendingFramesCount() == 0);
}

TEST_CASE("ExceptionUnwindTracker does not report a frame that catches an exception escaping its finally (.NET 9+)")
{
	// Method { static Method() { try { try { Thrower(); } finally { throw new ArgumentException(); } } catch (ArgumentException) { } } }
	Replay replay;
	replay.Throw()
		.Leave()
		.Enter(Thrower).Leave()
		.Enter(Method).Finally(Method)
		.Throw()
		.Enter(Method).Catch(Method);

	CHECK(replay.left == std::vector<FunctionID> { Thrower });
}

TEST_CASE("ExceptionUnwindTracker bounds frames that are never left")
{
	ExceptionUnwindTracker tracker;
	for (size_t index = 0; index < ExceptionUnwindTracker::MaxPendingFrames + 10; index++)
		tracker.OnUnwindFunctionEnter(Method);
	CHECK(tracker.GetPendingFramesCount() == ExceptionUnwindTracker::MaxPendingFrames);

	tracker.OnUnwindFunctionEnter(Thrower);
	CHECK(tracker.OnUnwindFunctionLeave() == Thrower);
}
