﻿using dnlib.DotNet.Emit;

namespace SharpDetect.Instrumentation.Stubs
{
    /// <summary>
    /// Instances of this class hold information about references to unfinished instructions
    /// (i.e. stubs that need to be filled prior to the compilation / method assembly)
    /// </summary>
    public class UnresolvedMethodStubs : Dictionary<Instruction, HelperOrWrapperReferenceStub>
    {
    }
}
