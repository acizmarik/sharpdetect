// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Iced.Intel;

namespace SharpDetect.Profiler.Hooks;

internal unsafe partial class AsmUtilities : IDisposable
{
    private record struct NakedEntryStub(IntPtr Pointer, DWORD Length, Action Dtor);
    private readonly List<NakedEntryStub> generatedStubs;
    private bool isDisposed = false;

    public AsmUtilities()
    {
        generatedStubs = new();
    }

    public IntPtr GenerateStub(IntPtr nakedMethodPtr)
    {
        var is64Bit = IntPtr.Size == 8;
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var stub = (is64Bit) ?
                EmitStubForNakedCall_Windows_X64(nakedMethodPtr) :
                EmitStubForNakedCall_Windows_X86(nakedMethodPtr);
            generatedStubs.Add(stub);
            return stub.Pointer;
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var stub = (is64Bit) ?
                EmitStubForNakedCall_Linux_X64(nakedMethodPtr) :
                EmitStubForNakedCall_Linux_X86(nakedMethodPtr);
            generatedStubs.Add(stub);
            return stub.Pointer;
        }
        else
        {
            throw new PlatformNotSupportedException(Environment.OSVersion.Platform.ToString());
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            foreach (var stub in generatedStubs)
            {
                // Free unmanaged memory
                stub.Dtor.Invoke();
            }
        }
    }
}
