using SharpDetect.Common.Exceptions;

namespace SharpDetect.Core.Runtime.Threads
{
    internal class OperationContext
    {
        private readonly Stack<ShadowObject?> fieldInstances;
        private readonly Stack<ShadowObject?> arrayInstances;
        private readonly Stack<int> arrayIndices;

        public OperationContext()
        {
            fieldInstances = new();
            arrayInstances = new();
            arrayIndices = new();
        }

        public ShadowObject? GetAndResetLastFieldInstance()
        {
            fieldInstances.TryPop(out var result);
            return result;
        }

        public void SetFieldInstance(ShadowObject? instance)
        {
            fieldInstances.Push(instance);
        }

        public ShadowObject? GetAndResetLastArrayInstance()
        {
            Guard.NotEmpty<ShadowObject?, ShadowRuntimeStateException>(arrayInstances);
            return arrayInstances.Pop();
        }

        public void SetArrayInstance(ShadowObject? instance)
        {
            arrayInstances.Push(instance);
        }

        public int? GetAndResetLastArrayIndex()
        {
            Guard.NotEmpty<int, ShadowRuntimeStateException>(arrayIndices);
            return arrayIndices.Pop();
        }

        public void SetArrayIndex(int index)
        {
            arrayIndices.Push(index);
        }
    }
}
