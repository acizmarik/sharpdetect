namespace SharpDetect.Profiler
{
    public readonly struct MdTypeDef
    {
        public static readonly MdTypeDef Nil = new(0);

        public readonly int Value;

        public MdTypeDef(int value)
        {
            Value = value;
        }
    }
}
