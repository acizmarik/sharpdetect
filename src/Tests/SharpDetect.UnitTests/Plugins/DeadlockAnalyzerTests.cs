using Microsoft.Extensions.Logging;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services.Reporting;
using SharpDetect.Core.Reporting;
using SharpDetect.Core.Runtime;
using SharpDetect.Core.Runtime.Threads;
using SharpDetect.Plugins;
using Xunit;

namespace SharpDetect.UnitTests.Plugins
{
    public class DeadlockAnalyzerTests : TestsBase
    {
        [Fact]
        public async Task DeadlockAnalyzerTests_SimpleDeadlock()
        {
            // Prepare
            const int pid = 123;
            var shadowCLR = new ShadowCLR(pid, null!, null!, null!);
            using var thread1 = new ShadowThread(pid, new UIntPtr(456), 1, new Core.Runtime.Scheduling.SchedulerBase.SchedulerEpochChangeSignaller());
            using var thread2 = new ShadowThread(pid, new UIntPtr(789), 2, new Core.Runtime.Scheduling.SchedulerBase.SchedulerEpochChangeSignaller());
            var plugin = new DeadlockAnalyzerPlugin();
            var reportingService = new ReportingService();
            var serviceProvider = BuildServiceProvider(
                (typeof(ILoggerFactory), LoggerFactory),
                (typeof(IReportingService), reportingService),
                (typeof(IReportsReaderProvider), reportingService));
            plugin.Initialize(serviceProvider);
            var shadowObj1 = new ShadowObject() { ShadowPointer = new(0x123456789) };
            var shadowObj2 = new ShadowObject() { ShadowPointer = new(0x987654321) };

            // Act
            // T1 acquires O1 (success)
            plugin.LockAcquireAttempted(shadowObj1, new EventInfo(shadowCLR, thread1));
            plugin.LockAcquireReturned(shadowObj1, true, new EventInfo(shadowCLR, thread1));
            shadowObj1.SyncBlock.Acquire(thread1);
            // T2 acquires O2 (success)
            plugin.LockAcquireAttempted(shadowObj2, new EventInfo(shadowCLR, thread2));
            plugin.LockAcquireReturned(shadowObj2, true, new EventInfo(shadowCLR, thread2));
            shadowObj2.SyncBlock.Acquire(thread2);
            // T1 attempts acquire O2 (blocked)
            plugin.LockAcquireAttempted(shadowObj2, new EventInfo(shadowCLR, thread1));
            // T2 attempts acquire O1 (blocked) -> Deadlock
            plugin.LockAcquireAttempted(shadowObj1, new EventInfo(shadowCLR, thread2));

            // Assert
            Assert.Equal(1, reportingService.ErrorCount);
            Assert.Equal(0, reportingService.WarningCount);
            Assert.Equal(0, reportingService.InformationCount);
            Assert.Equal(DeadlockAnalyzerPlugin.DiagnosticsCategory, (await reportingService.GetReportsReader().ReadAsync()).Category);
        }
    }
}
