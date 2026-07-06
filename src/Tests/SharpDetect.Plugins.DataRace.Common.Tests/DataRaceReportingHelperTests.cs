// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;
using Xunit;

namespace SharpDetect.Plugins.DataRace.Common.Tests;

public class DataRaceReportingHelperTests
{
    private const string UserModule = "/app/bin/MyApp.dll";
    private const string SystemModule = "/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.9/System.Private.CoreLib.dll";

    [Fact]
    public void BuildStackTraceSegments_NullStackTrace_ReturnsEmpty()
    {
        Assert.Empty(DataRaceReportingHelper.BuildStackTraceSegments(null));
    }

    [Fact]
    public void BuildStackTraceSegments_ConsecutiveSystemFrames_AreCollapsed()
    {
        var stackTrace = CreateStackTrace(
            CreateFrame(UserModule),
            CreateFrame(SystemModule),
            CreateFrame(SystemModule),
            CreateFrame(SystemModule));

        var segments = DataRaceReportingHelper.BuildStackTraceSegments(stackTrace);

        Assert.Equal(2, segments.Count);
        Assert.False((bool)GetProperty(segments[0], "isCollapsed"));
        Assert.True((bool)GetProperty(segments[1], "isCollapsed"));
        Assert.Equal(3, (int)GetProperty(segments[1], "count"));
    }

    [Fact]
    public void BuildStackTraceSegments_SingleSystemFrame_IsNotCollapsed()
    {
        var stackTrace = CreateStackTrace(
            CreateFrame(UserModule),
            CreateFrame(SystemModule),
            CreateFrame(UserModule));

        var segments = DataRaceReportingHelper.BuildStackTraceSegments(stackTrace);

        Assert.Equal(3, segments.Count);
        Assert.All(segments, segment => Assert.False((bool)GetProperty(segment, "isCollapsed")));
    }

    [Fact]
    public void BuildStackTraceSegments_SystemTopFrame_IsNeverCollapsed()
    {
        var stackTrace = CreateStackTrace(
            CreateFrame(SystemModule),
            CreateFrame(SystemModule),
            CreateFrame(SystemModule));

        var segments = DataRaceReportingHelper.BuildStackTraceSegments(stackTrace);

        Assert.Equal(2, segments.Count);
        Assert.False((bool)GetProperty(segments[0], "isCollapsed"));
        var topFrame = GetFrames(segments[0]).Single();
        Assert.True((bool)GetProperty(topFrame, "isTopFrame"));
        Assert.True((bool)GetProperty(topFrame, "isSystemFrame"));
        Assert.True((bool)GetProperty(segments[1], "isCollapsed"));
        Assert.Equal(2, (int)GetProperty(segments[1], "count"));
    }

    [Fact]
    public void BuildStackTraceSegments_FrameWithoutOffset_HasNullOffsetAndInstruction()
    {
        var stackTrace = CreateStackTrace(
            CreateFrame(UserModule, methodOffset: 5, instruction: "stsfld Test::Field"),
            CreateFrame(UserModule));

        var segments = DataRaceReportingHelper.BuildStackTraceSegments(stackTrace);

        var topFrame = GetFrames(segments[0]).Single();
        Assert.Equal("IL_0005", GetProperty(topFrame, "methodOffset"));
        Assert.Equal("stsfld Test::Field", GetProperty(topFrame, "instruction"));

        var deepFrame = GetFrames(segments[1]).Single();
        Assert.Null(GetPropertyOrNull(deepFrame, "methodOffset"));
        Assert.Null(GetPropertyOrNull(deepFrame, "instruction"));
    }

    [Fact]
    public void BuildStackTraceSegments_Frame_FormatsTokenAsHexAndShortensAssembly()
    {
        var stackTrace = CreateStackTrace(CreateFrame(SystemModule, methodToken: 0x06000006));

        var segments = DataRaceReportingHelper.BuildStackTraceSegments(stackTrace);

        var frame = GetFrames(segments.Single()).Single();
        Assert.Equal("0x06000006", GetProperty(frame, "metadataToken"));
        Assert.Equal("System.Private.CoreLib.dll", GetProperty(frame, "assemblyFileName"));
        Assert.Equal(SystemModule, GetProperty(frame, "assemblyPath"));
    }

    private static StackTrace CreateStackTrace(params StackFrame[] frames)
    {
        var threadInfo = new ThreadInfo(1, "T1");
        return new StackTrace(threadInfo, [.. frames]);
    }

    private static StackFrame CreateFrame(
        string modulePath,
        int methodToken = 0x06000001,
        uint? methodOffset = null,
        string? instruction = null)
    {
        return new StackFrame(
            MethodName: "System.Void Test::Method()",
            SourceMapping: modulePath,
            MethodToken: methodToken,
            MethodOffset: methodOffset,
            Instruction: instruction,
            SourceFileName: null,
            SourceLine: null,
            SourceCode: null);
    }

    private static IReadOnlyList<object> GetFrames(object segment)
    {
        return (object[])GetProperty(segment, "frames");
    }

    private static object GetProperty(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name);
        Assert.NotNull(property);
        return property.GetValue(instance)!;
    }

    private static object? GetPropertyOrNull(object instance, string name)
    {
        var property = instance.GetType().GetProperty(name);
        Assert.NotNull(property);
        return property.GetValue(instance);
    }
}
