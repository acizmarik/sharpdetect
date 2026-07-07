// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using Xunit;

namespace SharpDetect.Plugins.DataRace.Common.Tests;

public class DataRaceLoggerTests
{
    private const string UserModule = "/app/bin/MyApp.dll";
    private const string SystemModule = "/usr/lib/dotnet/shared/Microsoft.NETCore.App/10.0.9/System.Private.CoreLib.dll";

    [Fact]
    public void GetThreadDisplayName_NamedThread_ReturnsName()
    {
        var access = CreateAccess(threadId: 42, threadName: "T1");

        Assert.Equal("T1", DataRaceLogger.GetThreadDisplayName(access));
    }

    [Fact]
    public void GetThreadDisplayName_UnnamedThread_ReturnsHexThreadId()
    {
        var access = CreateAccess(threadId: 0x5BFD, threadName: null);

        Assert.Equal("Thread-0x5BFD", DataRaceLogger.GetThreadDisplayName(access));
    }

    [Fact]
    public void FormatStackTraceLines_FrameWithSourceInfo_FormatsFileAndLineWithoutSnippet()
    {
        var frame = CreateFrame(
            UserModule,
            methodName: "Program.<>c.<<Main>$>b__0_0()",
            methodOffset: 2,
            sourceFileName: "/app/Program.cs",
            sourceLine: 8,
            sourceCode: "        Test.Field = 1;");

        var lines = DataRaceLogger.FormatStackTraceLines([frame]);

        Assert.Equal("at Program.<>c.<<Main>$>b__0_0() in /app/Program.cs:8", lines.Single());
    }

    [Fact]
    public void FormatStackTraceLines_FrameWithoutSourceInfo_FallsBackToIlOffset()
    {
        var frame = CreateFrame(UserModule, methodName: "Program.Main()", methodOffset: 0x1A);

        var lines = DataRaceLogger.FormatStackTraceLines([frame]);

        Assert.Equal("at Program.Main():IL_001A", lines.Single());
    }

    [Fact]
    public void FormatStackTraceLines_DeepFrameWithoutOffset_ShowsMethodOnly()
    {
        var frames = new[]
        {
            CreateFrame(UserModule, methodName: "Program.Main()", methodOffset: 2),
            CreateFrame(UserModule, methodName: "Program.Caller()")
        };

        var lines = DataRaceLogger.FormatStackTraceLines(frames);

        Assert.Equal(2, lines.Count);
        Assert.Equal("at Program.Caller()", lines[1]);
    }

    [Fact]
    public void FormatStackTraceLines_ConsecutiveSystemFrames_AreSummarized()
    {
        var frames = new[]
        {
            CreateFrame(UserModule, methodName: "Program.Main()", methodOffset: 2),
            CreateFrame(SystemModule, methodName: "System.Threading.Thread.StartHelper.RunWorker()"),
            CreateFrame(SystemModule, methodName: "System.Threading.Thread.StartCallback()"),
            CreateFrame(SystemModule, methodName: "System.Threading.Thread.Start()")
        };

        var lines = DataRaceLogger.FormatStackTraceLines(frames);

        Assert.Equal(2, lines.Count);
        Assert.Equal("at System.Threading.Thread.StartHelper.RunWorker() (+2 more)", lines[1]);
    }

    [Fact]
    public void FormatStackTraceLines_SystemTopFrame_IsNotSummarized()
    {
        var frames = new[]
        {
            CreateFrame(SystemModule, methodName: "System.Threading.Monitor.Enter()", methodOffset: 2),
            CreateFrame(SystemModule, methodName: "System.Threading.Thread.StartCallback()")
        };

        var lines = DataRaceLogger.FormatStackTraceLines(frames);

        Assert.Equal(2, lines.Count);
        Assert.Equal("at System.Threading.Monitor.Enter():IL_0002", lines[0]);
        Assert.Equal("at System.Threading.Thread.StartCallback()", lines[1]);
    }

    private static StackFrame CreateFrame(
        string modulePath,
        string methodName,
        uint? methodOffset = null,
        string? sourceFileName = null,
        int? sourceLine = null,
        string? sourceCode = null)
    {
        return new StackFrame(
            MethodName: methodName,
            SourceMapping: modulePath,
            MethodToken: 0x06000001,
            MethodOffset: methodOffset,
            Instruction: null,
            SourceFileName: sourceFileName,
            SourceLine: sourceLine,
            SourceCode: sourceCode);
    }

    private static AccessInfo CreateAccess(nuint threadId, string? threadName)
    {
        var stack = new CapturedStackTrace(new CapturedStackFrame(new ModuleId(1), new MdMethodDef(0x06000001)));
        return new AccessInfo(
            ProcessThreadId: new ProcessThreadId(1, new ThreadId(threadId)),
            ThreadName: threadName,
            MethodOffset: 0,
            AccessType: AccessType.Write,
            Stack: stack);
    }
}
