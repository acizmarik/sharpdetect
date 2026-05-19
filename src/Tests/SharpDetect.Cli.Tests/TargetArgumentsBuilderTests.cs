// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands.Run;
using SharpDetect.Worker.Services;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class TargetArgumentsBuilderTests
{
    private static RunCommandArgs CreateArgs(TargetConfigurationArgs target)
        => new(
            Runtime: null,
            Target: target,
            Analysis: new AnalysisPluginConfigurationArgs(pluginName: "TestPlugin"));

    [Fact]
    public void Executable_Produces_Path_Only()
    {
        var args = CreateArgs(new TargetConfigurationArgs(path: "MyApp.dll"));

        var result = TargetArgumentsBuilder.Build(args);

        Assert.Equal(new[] { "MyApp.dll" }, result);
    }

    [Fact]
    public void Executable_Includes_Args_When_Provided()
    {
        var args = CreateArgs(new TargetConfigurationArgs(path: "MyApp.dll", args: "--foo"));

        var result = TargetArgumentsBuilder.Build(args);

        Assert.Equal(new[] { "MyApp.dll", "--foo" }, result);
    }

    [Fact]
    public void Mtp_TestAssembly_Without_Filter_Produces_Path_Only()
    {
        var args = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly,
            test: new TestTargetConfigurationArgs(runner: TestRunner.Mtp)));

        var result = TargetArgumentsBuilder.Build(args);

        Assert.Equal(new[] { "MyTests.dll" }, result);
    }

    [Fact]
    public void Mtp_TestAssembly_With_Filter_Emits_TreeNodeFilter()
    {
        var args = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly,
            test: new TestTargetConfigurationArgs(runner: TestRunner.Mtp, filter: "/*/*/*/RaceFact")));

        var result = TargetArgumentsBuilder.Build(args);

        Assert.Equal(
            new[] { "MyTests.dll", "--treenode-filter", "/*/*/*/RaceFact" },
            result);
    }

    [Fact]
    public void Mtp_TestAssembly_Appends_AdditionalRunnerArgs()
    {
        var args = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly,
            test: new TestTargetConfigurationArgs(
                runner: TestRunner.Mtp,
                filter: "/*/*/*/RaceFact",
                additionalRunnerArgs: "--report-trx")));

        var result = TargetArgumentsBuilder.Build(args);

        Assert.Equal(
            new[] { "MyTests.dll", "--treenode-filter", "/*/*/*/RaceFact", "--report-trx" },
            result);
    }

    [Fact]
    public void VsTest_TestAssembly_Emits_DotnetTest_Invocation_With_EnvFlags()
    {
        var args = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly,
            test: new TestTargetConfigurationArgs(runner: TestRunner.VsTest)));
        var env = new Dictionary<string, string>
        {
            ["CORECLR_ENABLE_PROFILING"] = "1",
            ["CORECLR_PROFILER"] = "{abc}"
        };

        var result = TargetArgumentsBuilder.Build(args, env);

        Assert.Equal(
            new[]
            {
                "test", "MyTests.dll",
                "-e", "CORECLR_ENABLE_PROFILING=1",
                "-e", "CORECLR_PROFILER={abc}"
            },
            result);
    }

    [Fact]
    public void VsTest_TestAssembly_With_Filter_Emits_FilterFlag()
    {
        var args = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly,
            test: new TestTargetConfigurationArgs(runner: TestRunner.VsTest, filter: "FullyQualifiedName~Foo")));

        var result = TargetArgumentsBuilder.Build(args);

        Assert.Equal(
            new[] { "test", "MyTests.dll", "--filter", "FullyQualifiedName~Foo" },
            result);
    }

    [Fact]
    public void RequiresEnvironmentInjection_Is_True_Only_For_VSTest()
    {
        var executable = CreateArgs(new TargetConfigurationArgs(path: "MyApp.dll"));
        var mtp = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly,
            test: new TestTargetConfigurationArgs(runner: TestRunner.Mtp)));
        var vstest = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly,
            test: new TestTargetConfigurationArgs(runner: TestRunner.VsTest)));

        Assert.False(TargetArgumentsBuilder.RequiresEnvironmentInjection(executable));
        Assert.False(TargetArgumentsBuilder.RequiresEnvironmentInjection(mtp));
        Assert.True(TargetArgumentsBuilder.RequiresEnvironmentInjection(vstest));
    }

    [Fact]
    public void TestAssembly_Without_TestConfig_Throws()
    {
        var args = CreateArgs(new TargetConfigurationArgs(
            path: "MyTests.dll",
            kind: TargetKind.TestAssembly));

        Assert.Throws<ArgumentException>(() => TargetArgumentsBuilder.Build(args));
    }
}
