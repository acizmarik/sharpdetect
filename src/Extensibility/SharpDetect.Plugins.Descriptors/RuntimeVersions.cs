// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public static class RuntimeVersions
{
    public static MethodVersionDescriptor Net8 { get; } = MethodVersionDescriptor.ForMajorVersions(8, 8);
    public static MethodVersionDescriptor Net8To9 { get; } = MethodVersionDescriptor.ForMajorVersions(8, 9);
    public static MethodVersionDescriptor Net8To10 { get; } = MethodVersionDescriptor.ForMajorVersions(8, 10);
    public static MethodVersionDescriptor Net9To10 { get; } = MethodVersionDescriptor.ForMajorVersions(9, 10);
    public static MethodVersionDescriptor Net9To11 { get; } = MethodVersionDescriptor.ForMajorVersions(9, 11);
    public static MethodVersionDescriptor Net10 { get; } = MethodVersionDescriptor.ForMajorVersions(10, 10);
    public static MethodVersionDescriptor Net10To11 { get; } = MethodVersionDescriptor.ForMajorVersions(10, 11);
    public static MethodVersionDescriptor Net11 { get; } = MethodVersionDescriptor.ForMajorVersions(11, 11);
}
