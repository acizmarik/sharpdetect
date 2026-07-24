// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Reporting;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class DataRacePluginTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_DataRacePluginTests";
    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Static_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Static_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Static_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Static_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Static_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Static_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Static_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Static_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Instance_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Instance_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Instance_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Instance_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Instance_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Instance_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Instance_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Instance_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Instance_WriteWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Instance_WriteWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Instance_WriteWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Instance_WriteWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Instance_SingleWriterWriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Instance_SingleWriterWriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Instance_SingleWriterWriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Instance_SingleWriterWriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_AutoProperty_Instance_PostPublicationWriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_AutoProperty_Instance_PostPublicationWriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_Static_WrittenInInstanceCtor_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_Static_WrittenInInstanceCtor_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_Static_AutoProperty_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_Static_AutoProperty_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ConstructorWrite_PublishThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ConstructorWrite_PublishThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ConstructorAutoPropertyWrite_PublishThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ConstructorAutoPropertyWrite_PublishThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task StaticCctorHelperWrite_WithoutStackTraces_IsReportedAsRace(string sdk, string plugin)
        => AssertDetectsDataRace(
            "Test_NoDataRace_StaticCctorHelperWrite_ConcurrentFirstAccess", sdk, plugin,
            enableStackTraceCollection: false);

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task StaticCctorHelperWrite_WithStackTraces_IsNotReported(string sdk, string plugin)
        => AssertDoesNotDetectDataRace(
            "Test_NoDataRace_StaticCctorHelperWrite_ConcurrentFirstAccess", sdk, plugin,
            enableStackTraceCollection: true);

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_CtorSetterWrite_PublishThenRead_WithoutStackTraces(string sdk, string plugin)
        => AssertDoesNotDetectDataRace(
            "Test_NoDataRace_CtorSetterWrite_PublishThenRead", sdk, plugin,
            enableStackTraceCollection: false);

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_CtorSetterWrite_PublishThenRead_WithStackTraces(string sdk, string plugin)
        => AssertDoesNotDetectDataRace(
            "Test_NoDataRace_CtorSetterWrite_PublishThenRead", sdk, plugin,
            enableStackTraceCollection: true);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_StaticHelperWrite_NotFromCctor_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_StaticHelperWrite_NotFromCctor_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ObjectInitializerThenPostPublicationWrite(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ObjectInitializerThenPostPublicationWrite", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ConcurrentDictionaryPublishThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ConcurrentDictionaryPublishThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ConcurrentDictionaryGetOrAddFactoryThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ConcurrentDictionaryGetOrAddFactoryThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ConcurrentDictionaryPostPublicationWrite(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ConcurrentDictionaryPostPublicationWrite", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ConcurrentDictionaryMissingKeyThrows(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ConcurrentDictionaryMissingKeyThrows", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_LazyPublishThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_LazyPublishThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_LazyValueTypePublishThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_LazyValueTypePublishThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_LazyFactoryThrows(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_LazyFactoryThrows", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_LazyPostPublicationWrite(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_LazyPostPublicationWrite", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ReferenceType_Static_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ReferenceType_Static_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ValueType_Static_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ValueType_Static_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ReferenceType_Instance_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ReferenceType_Instance_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ValueType_Instance_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ValueType_Instance_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ThreadStatic_ReferenceType(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ThreadStatic_ReferenceType", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ThreadStatic_ValueType(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ThreadStatic_ValueType", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ThreadStatic_ReadWrite(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ThreadStatic_ReadWrite", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileField_Static_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileField_Static_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileField_Instance_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileField_Instance_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatilePingPong_OrdersPlainFieldAccesses(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatilePingPong_OrdersPlainFieldAccesses", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_Task_SequentialTasks_WriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Task_SequentialTasks_WriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlim_ProtectedWriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlim_ProtectedWriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlimAsync_ProtectedWriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlimAsync_ProtectedWriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlimAsync_HighContention_WriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlimAsync_HighContention_WriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlimAsync_WithCancellationToken_ProtectedWriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlimAsync_WithCancellationToken_ProtectedWriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlimAsync_WithTimeout_ProtectedWriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlimAsync_WithTimeout_ProtectedWriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlimAsync_CanceledWait_NoSharedAccess(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlimAsync_CanceledWait_NoSharedAccess", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlimAsync_TimeoutExpires_NoSharedAccess(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlimAsync_TimeoutExpires_NoSharedAccess", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_Monitor_HighContention_WriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Monitor_HighContention_WriteRead", sdk, plugin);
    
    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlim_HighContention_WriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Semaphore_HighContention_WriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlim_BatchRelease_WriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Semaphore_BatchRelease_WriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_GenericType_StaticInitializer_DifferentInstantiations_WriteWrite_NoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_GenericType_StaticInitializer_DifferentInstantiations_WriteWrite_NoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_Mutex_ProtectedWriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Mutex_ProtectedWriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_Mutex_HighContention_WriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Mutex_HighContention_WriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_KernelSemaphore_ProtectedWriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_KernelSemaphore_ProtectedWriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_AutoResetEvent_WriteThenSet_WaitThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_AutoResetEvent_WriteThenSet_WaitThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ManualResetEvent_PublishThenRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ManualResetEvent_PublishThenRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ManualResetEvent_SetBeforeWrite_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ManualResetEvent_SetBeforeWrite_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SignalAndWait_PingPong(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SignalAndWait_PingPong", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_AbandonedMutex_WaiterStillOrdered(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_AbandonedMutex_WaiterStillOrdered", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_WaitAll_TwoEvents_JoinsBothPublishers(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_WaitAll_TwoEvents_JoinsBothPublishers", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_WaitAny_WinnerOrdersAccess(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_WaitAny_WinnerOrdersAccess", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_WaitAny_LoserNotOrdered(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_WaitAny_LoserNotOrdered", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_WaitAny_AbandonedMutex_WaiterStillOrdered(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_WaitAny_AbandonedMutex_WaiterStillOrdered", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public async Task CanRenderReport(string sdk, string pluginFullTypeName)
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.MinValue);
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_NoDataRace_ReferenceType_Static_CorrectLocks")
            .WithTestPlugin(pluginFullTypeName)
            .WithPluginConfiguration(new { SkipInstrumentationForAssemblies = SkipSystemAssemblies })
            .WithRenderReport()
            .Build(sdk, testOutput, additionalData, timeProvider);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var reportRenderer = services.GetRequiredService<IReportSummaryRenderer>();
        var reportWriter = services.GetRequiredService<IReportSummaryWriter>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var summary = plugin.CreateDiagnostics();
        var context = new SummaryRenderingContext(summary, plugin, plugin.ReportTemplates, ConfigurationJson: string.Empty);
        var exception = await Record.ExceptionAsync(() => reportWriter.Write(context, reportRenderer, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    private static readonly string[] SkipSystemAssemblies = ["System.", "Microsoft."];


    private static object CreateConfiguration(bool enableStackTraceCollection)
    {
        return enableStackTraceCollection
            ? new
            {
                SkipInstrumentationForAssemblies = SkipSystemAssemblies,
                EnableStackTraceCollection = true,
                StackTraceCollectionMaxDepth = 8,
                StackTraceCollectionForFields = Array.Empty<string>()
            }
            : new { SkipInstrumentationForAssemblies = SkipSystemAssemblies };
    }

    private async Task AssertDetectsDataRace(
        string subjectArgs,
        string sdk,
        string pluginFullTypeName,
        bool enableStackTraceCollection = false)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithTestPlugin(pluginFullTypeName)
            .WithPluginConfiguration(CreateConfiguration(enableStackTraceCollection))
            .Build(sdk, testOutput, additionalData);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.NotEmpty(subjectReports);
        var report = subjectReports.First();
        Assert.Equal(plugin.ReportCategory, report.Category);
    }

    private async Task AssertDoesNotDetectDataRace(
        string subjectArgs,
        string sdk,
        string pluginFullTypeName,
        bool enableStackTraceCollection = false)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithTestPlugin(pluginFullTypeName)
            .WithPluginConfiguration(CreateConfiguration(enableStackTraceCollection))
            .Build(sdk, testOutput, additionalData);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.Empty(subjectReports);
    }

    private ITestPlugin GetTestPlugin(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<TestFastTrackPlugin>()
            ?? throw new InvalidOperationException($"Plugin must be a {nameof(TestFastTrackPlugin)}.");
    }
}
