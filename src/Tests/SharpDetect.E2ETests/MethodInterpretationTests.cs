// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class MethodInterpretationTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "MethodInterpretationTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1_Release.json")]
#endif
    public async Task MethodInterpretation_Monitor_EnterExit_LockStatement(string configuration)
    {
        // Arrange
        var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enteredTest = false;
        var exitedTest = false;
        var insideTestMethod = false;
        var methods = new List<MethodDef>();
        var lockObjects = new HashSet<Lock>();
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = true;
                enteredTest = true;
            }
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = false;
                exitedTest = true;
            }
        };
        plugin.LockAcquireAttempted += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockAcquireReturned += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockReleased += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

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
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2_Release.json")]
#endif
    public async Task MethodInterpretation_Monitor_EnterExit_Unsafe(string configuration)
    {
        // Arrange
        var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enteredTest = false;
        var exitedTest = false;
        var insideTestMethod = false;
        var methods = new List<MethodDef>();
        var lockObjects = new HashSet<Lock>();
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = true;
                enteredTest = true;
            }
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = false;
                exitedTest = true;
            }
        };
        plugin.LockAcquireAttempted += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockAcquireReturned += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockReleased += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

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
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3_Release.json")]
#endif
    public async Task MethodInterpretation_Monitor_TryEnterExit(string configuration)
    {
        // Arrange
        var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enteredTest = false;
        var exitedTest = false;
        var insideTestMethod = false;
        var methods = new List<MethodDef>();
        var lockObjects = new HashSet<Lock>();
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = true;
                enteredTest = true;
            }
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = false;
                exitedTest = true;
            }
        };
        plugin.LockAcquireAttempted += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockAcquireReturned += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };
        plugin.LockReleased += e =>
        {
            if (insideTestMethod)
            {
                methods.Add(plugin.Resolve(new RecordedEventMetadata(e.ProcessId, e.ThreadId), e.ModuleId, e.MethodToken));
                lockObjects.Add(e.LockObj);
            }
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

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
    
        [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join1_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join2_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join3_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join1_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join2_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join3_Release.json")]
#endif
    public async Task MethodInterpretation_Thread_Join(string configuration)
    {
        // Arrange
        var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enteredTest = false;
        var exitedTest = false;
        var joinAttempted = false;
        var joinReturned = false;
        var insideTestMethod = false;
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = true;
                enteredTest = true;
            }
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = false;
                exitedTest = true;
            }
        };
        plugin.ThreadJoinAttempted += e =>
        {
            if (insideTestMethod)
                joinAttempted = true;
        };
        plugin.LockAcquireReturned += e =>
        {
            if (insideTestMethod)
                joinReturned = true;
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(enteredTest);
        Assert.True(exitedTest);
        Assert.True(joinAttempted);
        Assert.True(joinReturned);
    }
}
