// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Cli.Handlers;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.E2ETests.Utils;
using Xunit;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class MethodInterpretationTests
{
    private const string ConfigurationFolder = "MethodInterpretationTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1_Debug.json", "Test_MonitorMethods_EnterExit1")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1_Release.json", "Test_MonitorMethods_EnterExit1")]
#endif
    public async Task MethodInterpretation_Monitor_EnterExit_LockStatement(string configuration, string testMethod)
    {
        // Arrange
        var handler = RunCommandHandler.Create(configuration, typeof(TestHappensBeforePlugin));
        var services = handler.ServiceProvider;
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var enteredTest = false;
        var exitedTest = false;
        var methods = new List<MethodDef>();
        var lockObjects = new HashSet<Lock>();
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name == testMethod)
                enteredTest = true;
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name == testMethod)
                exitedTest = true;
        };
        plugin.LockAcquireAttempted += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockAcquireReturned += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockReleased += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };

        // Execute
        await handler.ExecuteAsync(null!);

        // Assert
        Assert.True(enteredTest);
        Assert.True(exitedTest);
        Assert.Single(lockObjects);
        Assert.Equivalent(new List<string>
        {
            "System.Void System.Threading.Monitor::ReliableEnter(System.Object,System.Boolean&)",
            "System.Void System.Threading.Monitor::ReliableEnter(System.Object,System.Boolean&)",
            "System.Void System.Threading.Monitor::Exit(System.Object)",
        }, methods.Select(m => m.FullName));
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2_Debug.json", "Test_MonitorMethods_EnterExit2")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2_Release.json", "Test_MonitorMethods_EnterExit2")]
#endif
    public async Task MethodInterpretation_Monitor_EnterExit_Unsafe(string configuration, string testMethod)
    {
        // Arrange
        var handler = RunCommandHandler.Create(configuration, typeof(TestHappensBeforePlugin));
        var services = handler.ServiceProvider;
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var enteredTest = false;
        var exitedTest = false;
        var methods = new List<MethodDef>();
        var lockObjects = new HashSet<Lock>();
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name == testMethod)
                enteredTest = true;
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name == testMethod)
                exitedTest = true;
        };
        plugin.LockAcquireAttempted += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockAcquireReturned += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockReleased += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };

        // Execute
        await handler.ExecuteAsync(null!);

        // Assert
        Assert.True(enteredTest);
        Assert.True(exitedTest);
        Assert.Single(lockObjects);
        Assert.Equivalent(new List<string>
        {
            "System.Void System.Threading.Monitor::Enter(System.Object)",
            "System.Void System.Threading.Monitor::Enter(System.Object)",
            "System.Void System.Threading.Monitor::Exit(System.Object)",
        }, methods.Select(m => m.FullName));
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1_Debug.json", "Test_MonitorMethods_TryEnterExit1")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2_Debug.json", "Test_MonitorMethods_TryEnterExit2")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3_Debug.json", "Test_MonitorMethods_TryEnterExit3")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1_Release.json", "Test_MonitorMethods_TryEnterExit1")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2_Release.json", "Test_MonitorMethods_TryEnterExit2")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3_Release.json", "Test_MonitorMethods_TryEnterExit3")]
#endif
    public async Task MethodInterpretation_Monitor_TryEnterExit(string configuration, string testMethod)
    {
        // Arrange
        var handler = RunCommandHandler.Create(configuration, typeof(TestHappensBeforePlugin));
        var services = handler.ServiceProvider;
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var enteredTest = false;
        var exitedTest = false;
        var methods = new List<MethodDef>();
        var lockObjects = new HashSet<Lock>();
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name == testMethod)
                enteredTest = true;
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name == testMethod)
                exitedTest = true;
        };
        plugin.LockAcquireAttempted += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockAcquireReturned += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockReleased += e =>
        {
            if (enteredTest && !exitedTest)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };

        // Execute
        await handler.ExecuteAsync(null!);

        // Assert
        Assert.True(enteredTest);
        Assert.True(exitedTest);
        Assert.Single(lockObjects);
        Assert.Equivalent(new List<string>
        {
            "System.Void System.Threading.Monitor::ReliableEnterTimeout(System.Object,System.Int32,System.Boolean&)",
            "System.Void System.Threading.Monitor::ReliableEnterTimeout(System.Object,System.Int32,System.Boolean&)",
            "System.Void System.Threading.Monitor::Exit(System.Object)",
        }, methods.Select(m => m.FullName));
    }
}
