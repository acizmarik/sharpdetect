using dnlib.DotNet;
using dnlib.DotNet.Writer;

namespace SharpDetect.Dnlib.Extensions
{
    public sealed class MethodWriter
    {
        public readonly ModuleMetadataCache MetadataCache;
        private const int CorILMethodFatHeader = 0x03;

        public MethodWriter(ModuleMetadataCache metadataCache)
        {
            MetadataCache = metadataCache;
        }

        public byte[] Write(ModuleDef module, MethodDef method, RewritingInfo rewritingInfo)
        {
            var metadata = MetadataCache.GetMetadata(module);
            var writer = new MethodBodyWriter(metadata, method.Body);
            writer.Write();
            var bytecode = writer.GetFullMethodBody();

            // We need to ensure that correct tokens are present
            FixReferences(bytecode, method, rewritingInfo);

            return bytecode;
        }

        private static int GetHeaderSize(byte[] bytecode)
        {
            // Header can be recognized based on the first byte of method
            // CorILMethod_Tiny has set flag (0x02) and its size is 1 byte
            // CorILMethod_Fat has set flags (0x03) and its size is determined by upper 4 bits of the second byte (in DWORDS)

            if ((bytecode[0] & CorILMethodFatHeader) == CorILMethodFatHeader)
            {
                // Method has fat header
                return (bytecode[1] >> 4) * 4;
            }
            else
            {
                // Method has tiny header
                return 1;
            }
        }

        private unsafe void FixReferences(byte[] bytecode, MethodDef method, RewritingInfo rewritingInfo)
        {
            if (rewritingInfo.Stubs.Count == 0)
                return;
            var fixedInstructionsCount = 0;

            // Enumerate bytecode (skipping header)
            fixed (byte* pbytes = &bytecode[GetHeaderSize(bytecode)])
            {
                byte* pointer = pbytes;

                // Enumerate individual instructions
                for (var i = 0; i < method.Body.Instructions.Count; i++)
                {
                    var instructionSize = method.Body.Instructions[i].GetSize();

                    // Check if we need to fix this instruction
                    if (rewritingInfo.Stubs.TryGetValue(method.Body.Instructions[i], out var token))
                    {
                        byte* ptoken = pointer + method.Body.Instructions[i].OpCode.Size;
                        // Set new token
                        *((uint*)ptoken) = token.Raw;
                        ++fixedInstructionsCount;
                    }

                    // Check if there are any instructions left to fix
                    if (fixedInstructionsCount >= rewritingInfo.Stubs.Count)
                        break;

                    // Go to next instruction
                    pointer += instructionSize;
                }
            }
        }
    }
}
