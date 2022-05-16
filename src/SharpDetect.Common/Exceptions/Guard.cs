using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpDetect.Common.Exceptions
{
    public static class Guard
    {
        private static readonly ImmutableDictionary<Type, Func<string, Exception>> lookup;

        static Guard()
        {
            var builder = new Dictionary<Type, Func<string, Exception>>()
            {
                { typeof(ArgumentException), m => new ArgumentException(m) },
                { typeof(ArgumentNullException), m => new ArgumentNullException(m) },
                { typeof(ShadowRuntimeStateException), m => new ShadowRuntimeStateException(m) },
                { typeof(InvalidOperationException), m => new InvalidOperationException(m) },
                { typeof(InvalidProgramException), m => new InvalidProgramException(m) }
            };
            lookup = builder.ToImmutableDictionary();
        }

        public static void Equal<TValue, TException>(TValue expected, TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
            where TException : Exception
        {
            if (!EqualityComparer<TValue>.Default.Equals(expected, actual))
                Throw<TException>($"Provided argument {expr} was evaluated to {actual}, which is not equal to {expected}.");
        }

        public static void NotEqual<TValue, TException>(TValue invalid, TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(invalid, actual))
                Throw<TException>($"Provided argument {expr} was evaluated to {actual}, which was equal to {invalid}.");
        }

        public static TValue NotNull<TValue, TException>([NotNull] TValue? value, [CallerArgumentExpression("value")] string? expr = null)
        {
            if (value == null)
                Throw<TException>($"Provided argument {expr} was evaluated to null.");
            return value;
        }

        public static void True<TException>(bool expression, [CallerArgumentExpression("expression")] string? expr = null)
        {
            if (!expression)
                Throw<TException>($"Provided argument {expr} was evaluated to {expression}.");
        }

        public static void False<TException>(bool expression, [CallerArgumentExpression("expression")] string? expr = null)
        {
            if (expression)
                Throw<TException>($"Provided argument {expr} was evaluated to {expression}.");
        }

        public static void NotReachable<TException>([CallerMemberName] string? member = null, [CallerFilePath] string? filePath = null)
        {
            Throw<TException>($"This code is not supposed to be reachable by {member} from {filePath}!");
        }

        [DoesNotReturn]
        private static void Throw<TException>(string? message)
        {
            throw lookup[typeof(TException)](message!);
        }
    }
}
