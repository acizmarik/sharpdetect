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
            // TODO: Linux support
            throw new PlatformNotSupportedException();
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    }

    private static byte[] CompileAsm(Assembler assembler)
    {
        using var memoryStream = new MemoryStream();
        var codeWriter = new StreamCodeWriter(memoryStream);
        assembler.Assemble(codeWriter, 0);
        return memoryStream.ToArray();
    }

    private static void FillMemory(byte[] code, IntPtr memoryPtr, DWORD memorySize)
    {
        fixed (byte* codePtr = code)
            Buffer.MemoryCopy(codePtr, memoryPtr.ToPointer(), memorySize, code.Length);
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
