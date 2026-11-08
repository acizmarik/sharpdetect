// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Cli.Output;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Worker;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class SummaryPrinterTests
{
    private const string UserModule = "/app/bin/Shop.dll";
    private const string SystemModule = "/dotnet/shared/System.Private.CoreLib.dll";

    [Fact]
    public void DataRace_PrintsEachAccessOnTheRowOfItsThread()
    {
        var summary = CreateSummary(
            "1 data race in 1 field",
            CreateRace(0, "Shop.Inventory._count",
                ("T3", "Read Shop.Inventory.Add()", [CreateFrame("Shop.Inventory.Add()", UserModule, "Inventory.cs", 11)]),
                ("T2 (Worker A)", "Write Shop.Inventory.Add()", [CreateFrame("Shop.Inventory.Add()", UserModule, "Inventory.cs", 12)])));

        var output = Print(summary);

        Assert.Equal(
            """

            FastTrack: 1 data race in 1 field
              1. Shop.Inventory._count  (1 race)
                 - T3             Read Shop.Inventory.Add()   Inventory.cs:11
                   T2 (Worker A)  Write Shop.Inventory.Add()  Inventory.cs:12
                   1 occurrence · instance field

            """,
            output,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void DataRace_WhenSourceWasModifiedSinceBuild_MarksTheLocation()
    {
        var staleFrame = CreateFrame("Shop.Inventory.Add()", UserModule, "Inventory.cs", 11) with
        {
            Source = new SourceLocation("/src/Inventory.cs", 11, SourceCodeSnippet.OutOfDate)
        };
        var summary = CreateSummary(
            "1 data race in 1 field",
            CreateRace(0, "Shop.Inventory._count",
                ("T1", "Write Shop.Inventory.Add()", [staleFrame]),
                ("T2", "Write Shop.Inventory.Add()", [CreateFrame("Shop.Inventory.Add()", UserModule, "Inventory.cs", 12)])));

        var output = Print(summary);

        Assert.Contains($"T1  Write Shop.Inventory.Add()  Inventory.cs:11 (modified since build){Environment.NewLine}", output);
        Assert.Contains($"T2  Write Shop.Inventory.Add()  Inventory.cs:12{Environment.NewLine}", output);
    }

    [Fact]
    public void DataRace_InSystemCodeWithoutSymbols_NamesTheAccessedMethodOnce()
    {
        const string listAdd = "System.Collections.Generic.List`1.Add(T)";
        StackFrame[] frames =
        [
            CreateFrame(listAdd, SystemModule),
            CreateFrame("Program.Worker()", UserModule)
        ];
        var summary = CreateSummary(
            "1 data race in 1 field",
            CreateRace(0, "System.Collections.Generic.List`1._size",
                ("T1", $"Write {listAdd}", frames),
                ("T2", $"Write {listAdd}", frames)));

        var output = Print(summary);

        Assert.Contains($"     - T1  Write {listAdd}{Environment.NewLine}", output);
        Assert.DoesNotContain("Program.Worker()", output);
    }

    [Fact]
    public void DataRace_WhenFieldsAreHidden_CountsHiddenRaces()
    {
        var summary = CreateSummary(
            "4 data races in 3 fields",
            CreateRace(0, "App.First", ("T1", "Write App.M()", []), ("T2", "Write App.M()", [])),
            CreateRace(1, "App.Second", ("T1", "Write App.M()", []), ("T2", "Write App.M()", [])),
            CreateRace(2, "App.Second", ("T1", "Read App.M()", []), ("T2", "Write App.M()", [])),
            CreateRace(3, "App.Third", ("T1", "Write App.M()", []), ("T2", "Write App.M()", [])));

        var output = Print(summary, maxTargets: 1);

        Assert.Contains($"  … 3 more in the report{Environment.NewLine}", output);
    }

    [Fact]
    public void DataRace_ListsEveryRaceOnAField()
    {
        var summary = CreateSummary(
            "2 data races in 1 field",
            CreateRace(0, "App.Field",
                ("T1", "Write App.A()", [CreateFrame("App.A()", UserModule, "App.cs", 13)]),
                ("T2", "Write App.B()", [CreateFrame("App.B()", UserModule, "App.cs", 20)])),
            CreateRace(1, "App.Field",
                ("T1", "Write App.A()", [CreateFrame("App.A()", UserModule, "App.cs", 14)]),
                ("T2", "Write App.B()", [CreateFrame("App.B()", UserModule, "App.cs", 21)])));

        var output = Print(summary);

        Assert.Equal(
            """

            FastTrack: 2 data races in 1 field
              1. App.Field  (2 races)
                 - T1  Write App.A()  App.cs:13
                   T2  Write App.B()  App.cs:20
                   1 occurrence · instance field
                 - T1  Write App.A()  App.cs:14
                   T2  Write App.B()  App.cs:21
                   1 occurrence · instance field

            """,
            output,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void DataRace_WhenRacesOnAFieldAreHidden_CountsThem()
    {
        var summary = CreateSummary(
            "4 data races in 1 field",
            CreateRace(0, "App.Field", ("T1", "Write App.M()", []), ("T2", "Write App.M()", [])),
            CreateRace(1, "App.Field", ("T1", "Read App.M()", []), ("T2", "Write App.M()", [])),
            CreateRace(2, "App.Field", ("T1", "Write App.N()", []), ("T2", "Write App.N()", [])),
            CreateRace(3, "App.Field", ("T1", "Read App.N()", []), ("T2", "Write App.N()", [])));

        var output = Print(summary, maxReportsPerTarget: 2);

        Assert.Equal(2, output.Split(Environment.NewLine).Count(line => line.StartsWith("     - ", StringComparison.Ordinal)));
        Assert.Contains($"     … 2 more in the report{Environment.NewLine}", output);
    }

    [Fact]
    public void DataRace_WhenAllFieldsAreShown_DoesNotClaimMoreFindings()
    {
        var summary = CreateSummary(
            "2 data races in 1 field",
            CreateRace(0, "App.Field", ("T1", "Write App.M()", []), ("T2", "Write App.M()", [])),
            CreateRace(1, "App.Field", ("T1", "Read App.M()", []), ("T2", "Write App.M()", [])));

        var output = Print(summary);

        Assert.Contains("  1. App.Field  (2 races)", output);
        Assert.DoesNotContain("more", output);
    }

    [Fact]
    public void Deadlock_PrintsWhatEachThreadWaitsForAtTheFirstUserFrame()
    {
        StackFrame[] frames =
        [
            CreateFrame("System.Threading.Monitor.Enter_Slowpath(System.Object)", SystemModule),
            CreateFrame("Program.Worker()", UserModule)
        ];
        var report = new ReportBuilder(0, "Deadlock", processId: 1, DateTime.UnixEpoch)
            .SetTitle("Deadlock between T1 and T2 (Subject)")
            .SetDescription("2 threads are blocked in a cycle.");
        AddThread(report, new ThreadInfo(1, "T1"), frames, "Waits for lock #7 held by T2 (Subject)");
        AddThread(report, new ThreadInfo(2, "T2 (Subject)"), frames, "Waits for T1 to finish");
        var summary = CreateSummary("1 deadlock", report.Build());

        var output = Print(summary, pluginName: "Deadlock");

        Assert.Equal(
            """

            Deadlock: 1 deadlock
              1. Deadlock between T1 and T2 (Subject)
                   T1            Waits for lock #7 held by T2 (Subject)  Program.Worker()
                   T2 (Subject)  Waits for T1 to finish                  Program.Worker()
                   2 threads are blocked in a cycle.

            """,
            output,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Deadlock_WithTheSameThreads_IsListedSeparately()
    {
        Report CreateDeadlock(int identifier)
        {
            var report = new ReportBuilder(identifier, "Deadlock", processId: 1, DateTime.UnixEpoch)
                .SetTitle("Deadlock between T1 and T2")
                .SetDescription("2 threads are blocked in a cycle.");
            AddThread(report, new ThreadInfo(1, "T1"), []);
            AddThread(report, new ThreadInfo(2, "T2"), []);
            return report.Build();
        }

        var output = Print(CreateSummary("2 deadlocks", CreateDeadlock(0), CreateDeadlock(1)), pluginName: "Deadlock");

        Assert.Contains($"  1. Deadlock between T1 and T2{Environment.NewLine}", output);
        Assert.Contains($"  2. Deadlock between T1 and T2{Environment.NewLine}", output);
    }

    private static string Print(
        Summary summary,
        string pluginName = "FastTrack",
        int maxTargets = SummaryPrinter.DefaultMaxTargets,
        int maxReportsPerTarget = SummaryPrinter.DefaultMaxReportsPerTarget)
    {
        using var writer = new StringWriter();
        SummaryPrinter.Print(writer, pluginName, summary, new AnalysisOutcome([]), reportPath: null, Verbosity.Normal, maxTargets, maxReportsPerTarget);
        return writer.ToString();
    }

    private static Summary CreateSummary(string title, params Report[] reports)
    {
        var builder = new SummaryBuilder(TimeProvider.System)
            .SetTitle(title)
            .SetDescription("Description");
        foreach (var report in reports)
            builder.AddReport(report);
        return builder.Build();
    }

    private static Report CreateRace(
        int identifier,
        string field,
        (string Thread, string Side, StackFrame[] Frames) earlier,
        (string Thread, string Side, StackFrame[] Frames) later)
    {
        var report = new ReportBuilder(identifier, "DataRace", processId: 1, DateTime.UnixEpoch)
            .SetTarget(field)
            .SetTitle($"{earlier.Side} ↔ {later.Side}")
            .SetDescription("1 occurrence · instance field");
        AddThread(report, new ThreadInfo(1, earlier.Thread, AccessIndex: 0), earlier.Frames);
        AddThread(report, new ThreadInfo(2, later.Thread, AccessIndex: 1), later.Frames);
        return report.Build();
    }

    private static void AddThread(ReportBuilder report, ThreadInfo thread, StackFrame[] frames, string? reason = null)
    {
        report.AddThread(thread);
        report.AddStackTrace(new StackTrace(thread, [.. frames]));
        if (reason is not null)
            report.AddReportReason(thread, reason);
    }

    private static StackFrame CreateFrame(string methodName, string modulePath, string? document = null, int line = 0)
    {
        return new StackFrame(
            MethodName: methodName,
            ModulePath: modulePath,
            MethodToken: new MdMethodDef(0x06000001),
            Il: null,
            Source: document is null ? null : new SourceLocation($"/src/{document}", line, SourceCodeSnippet.None));
    }
}
