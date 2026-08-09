// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Plugins.Descriptors;
using SharpDetect.Plugins.Descriptors.Intrinsics;
using SharpDetect.Plugins.Descriptors.Methods;
using SharpDetect.Plugins.Descriptors.Types;

namespace SharpDetect.E2ETests.Utils;

public record TestPluginAdditionalData(
    ImmutableArray<MethodDescriptor> MethodDescriptors,
    ImmutableArray<FieldAccessIntrinsicDescriptor> FieldAccessIntrinsicDescriptors,
    ImmutableArray<TypeInjectionDescriptor> TypeInjectionDescriptors,
    bool EnableFieldsAccessInstrumentation)
{
    private static ImmutableArray<FieldAccessIntrinsicDescriptor> GetAllFieldAccessIntrinsics() =>
    [
        ..InterlockedIntrinsicDescriptors.GetAllIntrinsics()
            .Concat(VolatileIntrinsicDescriptors.GetAllIntrinsics())
    ];

    public static TestPluginAdditionalData CreateWithFieldsAccessInstrumentationDisabled() =>
        new(
            MethodDescriptors: 
            [
                ..MonitorMethodDescriptors.GetAllMethods()
                    .Concat(LockMethodDescriptors.GetAllMethods())
                    .Concat(ThreadMethodDescriptors.GetAllMethods())
                    .Concat(TaskMethodDescriptors.GetAllMethods())
                    .Concat(AsyncMethodBuilderMethodDescriptors.GetAllMethods())
                    .Concat(SemaphoreSlimMethodDescriptors.GetAllMethods())
                    .Concat(WaitHandleMethodDescriptors.GetAllMethods())
                    .Concat(LazyMethodDescriptors.GetAllMethods())
                    .Concat(ConcurrentDictionaryMethodDescriptors.GetAllMethods())
                    .Concat(FieldAccessDescriptors.GetAllMethods())
                    .Concat(TestMethodDescriptors.GetAllTestMethods())
            ],
            FieldAccessIntrinsicDescriptors: GetAllFieldAccessIntrinsics(),
            TypeInjectionDescriptors: ImmutableArray<TypeInjectionDescriptor>.Empty,
            EnableFieldsAccessInstrumentation: false);
    
    public static TestPluginAdditionalData CreateWithFieldsAccessInstrumentationEnabled() =>
        new(
            MethodDescriptors: 
            [
                ..MonitorMethodDescriptors.GetAllMethods()
                    .Concat(LockMethodDescriptors.GetAllMethods())
                    .Concat(ThreadMethodDescriptors.GetAllMethods())
                    .Concat(TaskMethodDescriptors.GetAllMethods())
                    .Concat(AsyncMethodBuilderMethodDescriptors.GetAllMethods())
                    .Concat(SemaphoreSlimMethodDescriptors.GetAllMethods())
                    .Concat(WaitHandleMethodDescriptors.GetAllMethods())
                    .Concat(LazyMethodDescriptors.GetAllMethods())
                    .Concat(ConcurrentDictionaryMethodDescriptors.GetAllMethods())
                    .Concat(FieldAccessDescriptors.GetAllMethods())
                    .Concat(TestMethodDescriptors.GetAllTestMethods())
            ],
            FieldAccessIntrinsicDescriptors: GetAllFieldAccessIntrinsics(),
            TypeInjectionDescriptors:
            [
                ..SharpDetectHelperTypeDescriptors.GetAllTypes()
            ],
            EnableFieldsAccessInstrumentation: true);
}