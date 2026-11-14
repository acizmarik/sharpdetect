// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using Xunit;

namespace SharpDetect.Plugins.DataRace.Common.Tests;

public class DataRaceLineTests
{
    private const int WriteMethodToken = 0x06000010;
    private const int ReadMethodToken = 0x06000011;

    private readonly ModuleDefUser _module;

    public DataRaceLineTests()
    {
        _module = new ModuleDefUser("TestModule");
        var assembly = new AssemblyDefUser("TestAssembly", new Version(1, 0, 0, 0));
        assembly.Modules.Add(_module);
    }

    [Fact]
    public void FormatDataRaceLine_PairsEachAccessWithItsThreadAndSourceLine()
    {
        var race = CreateRace(CreateField("Counter"));
        StackFrame[] earlierFrames = [CreateFrame("Program.Writer()", "/src/app/Program.cs", 8)];
        StackFrame[] laterFrames = [CreateFrame("Program.Reader()", "/src/app/Program.cs", 13)];

        var line = DataRaceLogger.FormatDataRaceLine(race, earlierFrames, laterFrames);

        Assert.Equal(
            "data race on TestNamespace.HolderCounter.Counter: write by T1 at Program.cs:8 ↔ read by T2 at Program.cs:13",
            line);
    }

    [Fact]
    public void FormatDataRaceLine_WhenSourceWasModifiedSinceBuild_MarksTheLocation()
    {
        var race = CreateRace(CreateField("Counter"));
        var staleFrame = CreateFrame("Program.Writer()") with
        {
            Source = new SourceLocation("/app/Program.cs", 8, SourceCodeSnippet.OutOfDate)
        };

        var line = DataRaceLogger.FormatDataRaceLine(race, [staleFrame], [CreateFrame("Program.Reader()", "/app/Program.cs", 13)]);

        Assert.Equal(
            "data race on TestNamespace.HolderCounter.Counter: write by T1 at Program.cs:8 (modified since build) ↔ read by T2 at Program.cs:13",
            line);
    }

    [Fact]
    public void FormatDataRaceLine_WithoutSource_FallsBackToMethodName()
    {
        var race = CreateRace(CreateField("Counter"));

        var line = DataRaceLogger.FormatDataRaceLine(race, [CreateFrame("Program.Writer()")], []);

        Assert.EndsWith(": write by T1 at Program.Writer() ↔ read by T2 at <unresolved-method>", line);
    }

    private FieldId CreateField(string name)
    {
        var type = new TypeDefUser("TestNamespace", $"Holder{name}", _module.CorLibTypes.Object.TypeDefOrRef);
        _module.Types.Add(type);
        var field = new FieldDefUser(name, new FieldSig(_module.CorLibTypes.Int32), FieldAttributes.Public);
        type.Fields.Add(field);
        return FieldId.Create(1, field, declaringTypeHandle: 0);
    }

    private static StackFrame CreateFrame(string methodName, string? documentPath = null, int line = 0)
    {
        return new StackFrame(
            MethodName: methodName,
            ModulePath: "/app/bin/MyApp.dll",
            MethodToken: new MdMethodDef(WriteMethodToken),
            Il: null,
            Source: documentPath is null ? null : new SourceLocation(documentPath, line, SourceCodeSnippet.None));
    }

    private static DataRaceInfo CreateRace(
        FieldId field,
        int earlierToken = WriteMethodToken,
        int laterToken = ReadMethodToken,
        AccessType earlierType = AccessType.Write,
        AccessType laterType = AccessType.Read)
    {
        return new DataRaceInfo(
            ProcessId: 1,
            FieldId: field,
            ObjectId: null,
            CurrentAccess: CreateAccess(2, laterToken, laterType),
            LastAccess: CreateAccess(1, earlierToken, earlierType),
            Timestamp: DateTime.UnixEpoch);
    }

    private static AccessInfo CreateAccess(nuint threadId, int methodToken, AccessType accessType)
    {
        return new AccessInfo(
            ProcessThreadId: new ProcessThreadId(1, new ThreadId(threadId)),
            ThreadName: $"T{threadId}",
            MethodOffset: 0,
            AccessType: accessType,
            Stack: new CapturedStackTrace(new CapturedStackFrame(new ModuleId(1), new MdMethodDef(methodToken))));
    }
}
