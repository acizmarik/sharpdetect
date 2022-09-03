using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static SharpDetect.Common.Exceptions.Guard;

namespace SharpDetect.Common.Exceptions
{
    public static class Guard
    {
        private static readonly ImmutableDictionary<Type, Func<string, Exception>> lookup;

        static Guard()
        {
            var builder = new Dictionary<Type, Func<string, Exception>>()
            {
                { typeof(ArgumentException), static m => new ArgumentException(m) },
                { typeof(ArgumentNullException), static m => new ArgumentNullException(m) },
                { typeof(ShadowRuntimeStateException), static m => new ShadowRuntimeStateException(m) },
                { typeof(InvalidOperationException), static m => new InvalidOperationException(m) },
                { typeof(InvalidProgramException), static m => new InvalidProgramException(m) }
            };
            lookup = builder.ToImmutableDictionary();
        }

        public static void Equal<TValue, TException>(TValue expected, TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
            where TException : Exception
        {
            if (!EqualityComparer<TValue>.Default.Equals(expected, actual))
                Throw<TException>($"Provided argument {expr} was evaluated to {actual}, which is not equal to {expected}.");
        }

        public static GuardGreaterThanComparer<TValue, TException> Greater<TValue, TException>(TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            return new GuardGreaterThanComparer<TValue, TException>(actual, expr!);
        }

        public static GuardGreaterOrEqualThanComparer<TValue, TException> GreaterOrEqual<TValue, TException>(TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            return new GuardGreaterOrEqualThanComparer<TValue, TException>(actual, expr!);
        }

        public static GuardLessThanComparer<TValue, TException> Less<TValue, TException>(TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            return new GuardLessThanComparer<TValue, TException>(actual, expr!);
        }

        public static GuardLessOrEqualThanComparer<TValue, TException> LessOrEqual<TValue, TException>(TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            return new GuardLessOrEqualThanComparer<TValue, TException>(actual, expr!);
        }

        public static void NotEqual<TValue, TException>(TValue invalid, TValue actual, [CallerArgumentExpression("actual")] string? expr = null)
        {
            if (EqualityComparer<TValue>.Default.Equals(invalid, actual))
                Throw<TException>($"Provided argument {expr} was evaluated to {actual}, which was equal to {invalid}.");
        }

        public static void NotEmpty<TValue, TException>(IEnumerable<TValue> collection, [CallerArgumentExpression("collection")] string? expr = null)
        {
            if ((collection.TryGetNonEnumeratedCount(out var count) && count == 0) || !collection.Any())
                Throw<TException>($"Provided collection {expr} was empty.");
        }

        public static void Single<TException>(ICollection collection, [CallerArgumentExpression("collection")] string? expr = null)
        {
            if (collection.Count != 1)
                Throw<TException>($"Provided collection {expr} does not have a single element.");
        }

        public static TValue NotNull<TValue, TException>([NotNull] TValue? value, [CallerArgumentExpression("value")] string? expr = null)
        {
            if (value == null)
                Throw<TException>($"Provided argument {expr} was evaluated to null.");
            return value;
        }

        public static void Null<TValue, TException>(TValue? value, [CallerArgumentExpression("value")] string? expr = null)
        {
            if (value != null)
                Throw<TException>($"Provided argument {expr} was not null.");
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
        internal static void Throw<TException>(string? message)
        {
            throw lookup[typeof(TException)](message!);
        }

        public record struct GuardGreaterThanComparer<TValue, TException>(TValue Value, string Expression);
        public record struct GuardLessThanComparer<TValue, TException>(TValue Value, string Expression);
        public record struct GuardGreaterOrEqualThanComparer<TValue, TException>(TValue Value, string Expression);
        public record struct GuardLessOrEqualThanComparer<TValue, TException>(TValue Value, string Expression);
    }

    public static class GuardCompareExtensions
    {
        public static void Than<TValue, TException>(this GuardGreaterThanComparer<TValue, TException> left, TValue right)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            if (left.Value.CompareTo(right) <= 0)
                Throw<TException>($"Provided argument {left.Expression} was not greater than {right}.");
        }

        public static void Than<TValue, TException>(this GuardGreaterOrEqualThanComparer<TValue, TException> left, TValue right)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            if (left.Value.CompareTo(right) < 0)
                Throw<TException>($"Provided argument {left.Expression} was not greater or equal than {right}.");
        }

        public static void Than<TValue, TException>(this GuardLessThanComparer<TValue, TException> left, TValue right)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            if (left.Value.CompareTo(right) >= 0)
                Throw<TException>($"Provided argument {left.Expression} was not lesser than {right}.");
        }

        public static void Than<TValue, TException>(this GuardLessOrEqualThanComparer<TValue, TException> left, TValue right)
            where TException : Exception
            where TValue : IComparable<TValue>
        {
            if (left.Value.CompareTo(right) > 0)
                Throw<TException>($"Provided argument {left.Expression} was not lesser or equal than {right}.");
        }
    }
}
