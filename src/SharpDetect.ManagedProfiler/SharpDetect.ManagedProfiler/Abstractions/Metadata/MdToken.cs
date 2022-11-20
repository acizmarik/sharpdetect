namespace SharpDetect.Profiler
{
    public readonly struct MdToken
    {
        public static readonly MdToken Nil = new(0);

        public readonly int Value;

        public MdToken(int value)
        {
            Value = value;
        }
    }
}
