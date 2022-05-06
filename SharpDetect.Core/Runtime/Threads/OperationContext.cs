namespace SharpDetect.Core.Runtime.Threads
{
    internal class OperationContext
    {
        public ShadowObject? FieldInstance { get; set; }
        public ShadowObject? ArrayInstance { get; set; }
        public int? ArrayIndex { get; set; }
    }
}
