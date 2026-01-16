// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Plugins.Descriptors;
using SharpDetect.Plugins.Descriptors.Methods;
using SharpDetect.Plugins.Descriptors.Types;

namespace SharpDetect.E2ETests.Utils;

public record TestPluginAdditionalData(
    ImmutableArray<MethodDescriptor> MethodDescriptors,
    ImmutableArray<TypeInjectionDescriptor> TypeInjectionDescriptors,
    bool EnableFieldsAccessInstrumentation)
{
    public static TestPluginAdditionalData CreateWithFieldsAccessInstrumentationDisabled() =>
        new(
            MethodDescriptors: 
            [
                ..MonitorMethodDescriptors.GetAllMethods()
                    .Concat(LockMethodDescriptors.GetAllMethods())
                    .Concat(ThreadMethodDescriptors.GetAllMethods())
                    .Concat(FieldAccessDescriptors.GetAllMethods())
                    .Concat(TestMethodDescriptors.GetAllTestMethods())
            ],
            TypeInjectionDescriptors: ImmutableArray<TypeInjectionDescriptor>.Empty,
            EnableFieldsAccessInstrumentation: false);
    
    public static TestPluginAdditionalData CreateWithFieldsAccessInstrumentationEnabled() =>
        new(
            MethodDescriptors: 
            [
                ..MonitorMethodDescriptors.GetAllMethods()
                    .Concat(LockMethodDescriptors.GetAllMethods())
                    .Concat(ThreadMethodDescriptors.GetAllMethods())
                    .Concat(FieldAccessDescriptors.GetAllMethods())
                    .Concat(TestMethodDescriptors.GetAllTestMethods())
            ],
            TypeInjectionDescriptors: 
            [
                ..SharpDetectHelperTypeDescriptors.GetAllTypes()
            ],
            EnableFieldsAccessInstrumentation: true);
}