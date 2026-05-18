// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Worker.Services;

public static class TargetArgumentsBuilder
{
    public static List<string> Build(
        RunCommandArgs arguments,
        IReadOnlyDictionary<string, string>? environmentVariables = null) => arguments.Target.Kind switch
    {
        TargetKind.Executable => BuildExecutableArguments(arguments),
        TargetKind.TestAssembly => arguments.Target.Test?.Runner switch
        {
            TestRunner.Mtp => BuildMtpTestArguments(arguments),
            TestRunner.VsTest => BuildVsTestArguments(arguments, environmentVariables ?? EmptyEnv),
            _ => throw new ArgumentException($"Target.Test must be specified when Target.Kind is \"{TargetKind.TestAssembly}\".")
        },
        _ => throw new ArgumentOutOfRangeException(nameof(arguments))
    };

    private static readonly IReadOnlyDictionary<string, string> EmptyEnv =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public static bool RequiresEnvironmentInjection(RunCommandArgs arguments) =>
        arguments.Target is { Kind: TargetKind.TestAssembly, Test.Runner: TestRunner.VsTest };

    private static List<string> BuildExecutableArguments(RunCommandArgs arguments)
    {
        var argsBuilder = new List<string>(capacity: 3);
        if (arguments.Runtime.Host?.Args is { } hostArgs)
            argsBuilder.Add(hostArgs);
        
        argsBuilder.Add(arguments.Target.Path);
        if (arguments.Target.Args is { } targetArgs)
            argsBuilder.Add(targetArgs);
        
        return argsBuilder;
    }

    private static List<string> BuildMtpTestArguments(RunCommandArgs arguments)
    {
        var test = arguments.Target.Test!;
        var argsBuilder = new List<string>(capacity: 5);
        if (arguments.Runtime.Host?.Args is { } hostArgs)
            argsBuilder.Add(hostArgs);
        
        argsBuilder.Add(arguments.Target.Path);
        
        if (test.Filter is { } filter)
        {
            argsBuilder.Add("--treenode-filter");
            argsBuilder.Add(filter);
        }
        
        if (test.AdditionalRunnerArgs is { } extra)
            argsBuilder.Add(extra);
        
        if (arguments.Target.Args is { } targetArgs)
            argsBuilder.Add(targetArgs);
        
        return argsBuilder;
    }

    private static List<string> BuildVsTestArguments(
        RunCommandArgs arguments,
        IReadOnlyDictionary<string, string> environmentVariables)
    {
        var test = arguments.Target.Test!;
        var argsBuilder = new List<string>(capacity: 8 + environmentVariables.Count * 2);
        if (arguments.Runtime.Host?.Args is { } hostArgs)
            argsBuilder.Add(hostArgs);
        
        argsBuilder.Add("test");
        argsBuilder.Add(arguments.Target.Path);
        
        foreach (var (key, value) in environmentVariables)
        {
            argsBuilder.Add("-e");
            argsBuilder.Add($"{key}={value}");
        }
        
        if (test.Filter is { } filter)
        {
            argsBuilder.Add("--filter");
            argsBuilder.Add(filter);
        }
        
        if (test.AdditionalRunnerArgs is { } extra)
            argsBuilder.Add(extra);
        
        if (arguments.Target.Args is { } targetArgs)
            argsBuilder.Add(targetArgs);
        
        return argsBuilder;
    }
}
