using dnlib.DotNet;
using SharpDetect.Common.Messages;

namespace SharpDetect.Instrumentation.Stubs
{
    public struct HelperOrWrapperReferenceStub
    {
        private MethodType? helperMethodType;
        private IMethodDefOrRef? externMethod;

        private HelperOrWrapperReferenceStub(MethodType? helperMethodType = null, IMethodDefOrRef? externMethod = null)
        {
            this.helperMethodType = helperMethodType;
            this.externMethod = externMethod;
        }

        public static HelperOrWrapperReferenceStub CreateHelperMethodReferenceStub(MethodType type)
            => new(helperMethodType: type);

        public static HelperOrWrapperReferenceStub CreateWrapperMethodReferenceStub(IMethodDefOrRef method)
            => new(externMethod: method);

        public bool IsHelperMethodReferenceStub() => helperMethodType.HasValue;
        public bool IsWrapperMethodReferenceStub() => externMethod is not null;

        public MethodType GetHelperMethodType()
        {
            return helperMethodType!.Value;
        }

        public IMethodDefOrRef GetExternMethod()
        {
            return externMethod!;
        }
    }
}
