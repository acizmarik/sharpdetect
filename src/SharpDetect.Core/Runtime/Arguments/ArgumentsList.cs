using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using System.Collections;

namespace SharpDetect.Core.Runtime.Arguments
{
    public struct ArgumentsList : IArgumentsList
    {
        private readonly (ushort, IValueOrObject)[] args;

        public ArgumentsList((ushort, IValueOrObject)[] args)
        {
            this.args = args;
        }

        internal (ushort, IValueOrObject)[] Raw { get => args; }

        public (ushort Index, IValueOrObject Argument) this[int index] { get => args[index]; }

        public IEnumerator<(ushort Index, IValueOrObject Argument)> GetEnumerator()
        {
            for (var i = 0; i < args.Length; i++)
                yield return (args[i].Item1, args[i].Item2);
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
