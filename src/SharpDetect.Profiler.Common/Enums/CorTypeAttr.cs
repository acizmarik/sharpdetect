﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[Flags]
public enum CorTypeAttr : uint
{
    tdVisibilityMask = 0x00000007,
    tdNotPublic = 0x00000000,
    tdPublic = 0x00000001,
    tdNestedPublic = 0x00000002,
    tdNestedPrivate = 0x00000003,
    tdNestedFamily = 0x00000004,
    tdNestedAssembly = 0x00000005,
    tdNestedFamANDAssem = 0x00000006,
    tdNestedFamORAssem = 0x00000007,

    tdLayoutMask = 0x00000018,
    tdAutoLayout = 0x00000000,
    tdSequentialLayout = 0x00000008,
    tdExplicitLayout = 0x00000010,

    tdClassSemanticsMask = 0x00000020,
    tdClass = 0x00000000,
    tdInterface = 0x00000020,

    tdAbstract = 0x00000080,
    tdSealed = 0x00000100,
    tdSpecialName = 0x00000400,

    tdImport = 0x00001000,
    tdSerializable = 0x00002000,
    tdWindowsRuntime = 0x00004000,

    tdStringFormatMask = 0x00030000,
    tdAnsiClass = 0x00000000,
    tdUnicodeClass = 0x00010000,
    tdAutoClass = 0x00020000,
    tdCustomFormatClass = 0x00030000,
    tdCustomFormatMask = 0x00C00000,

    tdBeforeFieldInit = 0x00100000,
    tdForwarder = 0x00200000,

    tdReservedMask = 0x00040800,
    tdRTSpecialName = 0x00000800,
    tdHasSecurity = 0x00040000,
}
