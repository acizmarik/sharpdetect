// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.Common.Unsafe
{
    public static class UnsafeHelpers
    {
        public static TStruct[] AsStructArray<TStruct>(ReadOnlySpan<byte> data) where TStruct : struct
        {
            var size = Marshal.SizeOf(typeof(TStruct));
            var recordsCount = data.Length / size;
            var ranges = new TStruct[recordsCount];
            for (var i = 0; i < recordsCount; i++)
            {
                ranges[i] = MemoryMarshal.Read<TStruct>(data);
                data = data[size..];
            }

            return ranges;
        }
    }
}
