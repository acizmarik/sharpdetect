// Copyright 2025 Andrej Čižmárik and Contributors
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
}