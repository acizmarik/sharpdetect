// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public record MethodVersionDescriptor(
    int FromMajorVersion,
    int FromMinorVersion,
    int FromBuildVersion,
    int ToMajorVersion,
    int ToMinorVersion,
    int ToBuildVersion)
{
    public static MethodVersionDescriptor Create(Version from, Version to)
    {
        return new MethodVersionDescriptor(
            from.Major,
            from.Minor,
            from.Build,
            to.Major,
            to.Minor,
            to.Build);
    }

    public static MethodVersionDescriptor ForMajorVersions(int firstMajor, int lastMajor)
    {
        return new MethodVersionDescriptor(
            firstMajor,
            FromMinorVersion: 0,
            FromBuildVersion: 0,
            lastMajor,
            ToMinorVersion: int.MaxValue,
            ToBuildVersion: int.MaxValue);
    }
}
