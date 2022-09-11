namespace SharpDetect.Core.Runtime.Threads
{
    internal class OperationContext
    {
        private ShadowObject? fieldInstance, arrayInstance;
        private int? arrayIndex;

        public ShadowObject? GetAndResetLastFieldInstance()
        {
            var result = fieldInstance;
            fieldInstance = null;
            return result;
        }

        public void SetFieldInstance(ShadowObject? instance)
        {
            fieldInstance = instance;
        }

        public ShadowObject? GetAndResetLastArrayInstance()
        {
            var result = arrayInstance;
            arrayInstance = null;
            return result;
        }

        public void SetArrayInstance(ShadowObject? instance)
        {
            arrayInstance = instance;
        }

        public int? GetAndResetLastArrayIndex()
        {
            var result = arrayIndex;
            arrayIndex = null;
            return result;
        }

        public void SetArrayIndex(int? index)
        {
            arrayIndex = index;
        }
    }
}
