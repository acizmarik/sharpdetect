using SharpDetect.Common.LibraryDescriptors;

namespace SharpDetect.Core.Models.CoreLibrary
{
    internal partial class CoreLibDescriptor : ILibraryDescriptor
    {
        public bool IsCoreLibrary => true;
        public string AssemblyName => "System.Private.CoreLib";

        public IEnumerable<string> GetAssemblyDependencies()
        {
            // BCL has no managed dependencies
            yield break;
        }

        public IEnumerable<(MethodIdentifier, MethodInterpretationData)> GetMethods()
        {
            return GetMonitorEnterExitMethods().Concat(GetMonitorWaitPulseMethods()).Concat(GetInjectedMethods());
        }
    }
}
