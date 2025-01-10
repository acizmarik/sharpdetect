// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Cli.Handlers;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Extensibility.Models;
using Xunit;

namespace SharpDetect.E2ETests
{
    [Collection("E2E")]
    public class ObjectTrackingTests
    {
        private const string ConfigurationFolder = "ObjectTrackingTestConfigurations";

        [Theory]
#if DEBUG
        [InlineData($"{ConfigurationFolder}/ObjectTracking_SingleGC_Debug.json", "Test_SingleGarbageCollection_ObjectTracking_Simple")]
        [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Debug.json", "Test_MultipleGarbageCollection_ObjectTracking_Simple")]
        [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Compacting_Debug.json", "Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject")]
#elif RELEASE
        [InlineData($"{ConfigurationFolder}/ObjectTracking_SingleGC_Release.json", "Test_SingleGarbageCollection_ObjectTracking_Simple")]
        [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Release.json", "Test_MultipleGarbageCollection_ObjectTracking_Simple")]
        [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Compacting_Release.json", "Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject")]
#endif
        public async Task ObjectsTracking(string configuration, string testMethod)
        {
            // Arrange
            var handler = RunCommandHandler.Create(configuration, typeof(TestHappensBeforePlugin));
            var services = handler.ServiceProvider;
            var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
            var enteredTest = false;
            var exitedTest = false;
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
                    lockObjects.Add(e.LockObj);
            };
            plugin.LockAcquireReturned += e =>
            {
                if (enteredTest && !exitedTest)
                    lockObjects.Add(e.LockObj);
            };
            plugin.LockReleased += e =>
            {
                if (enteredTest && !exitedTest)
                    lockObjects.Add(e.LockObj);
            };

            // Execute
            await handler.ExecuteAsync(null!);

            // Assert
            Assert.True(enteredTest);
            Assert.True(exitedTest);
            Assert.Single(lockObjects);
        }
    }
}
