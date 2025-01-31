// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events.Profiler;

public enum CorCallingConvention
{
    IMAGE_CEE_CS_CALLCONV_DEFAULT = 0x0,
    IMAGE_CEE_CS_CALLCONV_VARARG = 0x5,
    IMAGE_CEE_CS_CALLCONV_FIELD = 0x6,
    IMAGE_CEE_CS_CALLCONV_LOCAL_SIG = 0x7,
    IMAGE_CEE_CS_CALLCONV_PROPERTY = 0x8,
    IMAGE_CEE_CS_CALLCONV_UNMGD = 0x9,
    IMAGE_CEE_CS_CALLCONV_GENERICINST = 0xa,
    IMAGE_CEE_CS_CALLCONV_NATIVEVARARG = 0xb,
    IMAGE_CEE_CS_CALLCONV_MAX = 0xc,
    IMAGE_CEE_CS_CALLCONV_MASK = 0x0f,
    IMAGE_CEE_CS_CALLCONV_HASTHIS = 0x20,
    IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS = 0x40,
    IMAGE_CEE_CS_CALLCONV_GENERIC = 0x10,
}
