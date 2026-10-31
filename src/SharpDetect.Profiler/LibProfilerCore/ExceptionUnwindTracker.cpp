// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>

#include "ExceptionUnwindTracker.h"

std::optional<FunctionID> LibProfiler::ExceptionUnwindTracker::OnExceptionThrown()
{
    const auto leftFunctionId = PopFrameLeftByUncaughtException();
    _phase = Phase::Search;
    return leftFunctionId;
}

void LibProfiler::ExceptionUnwindTracker::OnSearchFilterEnter(const FunctionID functionId)
{
    _filters.push_back({ .functionId = functionId, .frameDepth = _frames.size() });
}

void LibProfiler::ExceptionUnwindTracker::OnSearchFilterLeave()
{
    if (_filters.empty())
        return;

    _frames.resize(std::min(_frames.size(), _filters.back().frameDepth));
    _filters.pop_back();
    _phase = Phase::Search;
}

void LibProfiler::ExceptionUnwindTracker::OnUnwindFunctionEnter(const FunctionID functionId)
{
    if (_phase == Phase::Search)
        _phase = Phase::Unwind;

    if (_frames.size() >= MaxPendingFrames)
        DropOldestFrame();

    const auto uncaught = _phase == Phase::UncaughtUnwind;
    if (uncaught && !IsRunningFilterOf(functionId))
        AbandonFinallyOf(functionId);

    _frames.push_back({ .functionId = functionId, .runsFinally = false, .uncaught = uncaught });
}

void LibProfiler::ExceptionUnwindTracker::OnUnwindFinallyEnter(const FunctionID functionId)
{
    if (TopFrameIs(functionId))
        _frames.back().runsFinally = true;
}

void LibProfiler::ExceptionUnwindTracker::OnUnwindFinallyLeave()
{
    if (!_frames.empty())
        _frames.back().runsFinally = false;
}

std::optional<FunctionID> LibProfiler::ExceptionUnwindTracker::OnUnwindFunctionLeave()
{
    // .NET 9+ reports the search leave of the last searched frame as an unwind leave when no catcher is found
    if (_phase == Phase::Search)
    {
        _phase = Phase::UncaughtUnwind;
        return std::nullopt;
    }

    if (_frames.empty())
        return std::nullopt;

    const auto frame = _frames.back();
    _frames.pop_back();
    _phase = frame.uncaught ? Phase::UncaughtUnwind : Phase::Unwind;

    if (IsRunningFilterOf(frame.functionId))
        return std::nullopt;

    AbandonFinallyOf(frame.functionId);
    return frame.functionId;
}

void LibProfiler::ExceptionUnwindTracker::OnCatcherEnter(const FunctionID functionId)
{
    if (TopFrameIs(functionId))
        _frames.pop_back();
}

bool LibProfiler::ExceptionUnwindTracker::TopFrameIs(const FunctionID functionId) const
{
    return !_frames.empty() && _frames.back().functionId == functionId;
}

bool LibProfiler::ExceptionUnwindTracker::IsRunningFilterOf(const FunctionID functionId) const
{
    return !_filters.empty() &&
        _filters.back().functionId == functionId &&
        _filters.back().frameDepth == _frames.size();
}

std::optional<FunctionID> LibProfiler::ExceptionUnwindTracker::PopFrameLeftByUncaughtException()
{
    if (_frames.empty() || !_frames.back().uncaught || _frames.back().runsFinally)
        return std::nullopt;

    const auto functionId = _frames.back().functionId;
    _frames.pop_back();
    return functionId;
}

void LibProfiler::ExceptionUnwindTracker::AbandonFinallyOf(const FunctionID functionId)
{
    if (TopFrameIs(functionId) && _frames.back().runsFinally)
        _frames.pop_back();
}

void LibProfiler::ExceptionUnwindTracker::DropOldestFrame()
{
    _frames.erase(_frames.begin());
    for (auto& filter : _filters)
    {
        if (filter.frameDepth > 0)
            filter.frameDepth--;
    }
}
