using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpDetect.Common.Exceptions
{
    public static class ThrowHelpers
    {
        private static readonly Dictionary<Type, Func<string, Exception>> lookup = new()
        {
            { typeof(ArgumentException), message => new ArgumentException(message) },
            { typeof(FileNotFoundException), message => new FileNotFoundException(message) }
        };

        public static void ThrowIf<TException>(bool condition, [CallerArgumentExpression("condition")] string? expr = null)
            where TException : Exception, new()
        {
            if (condition)
                Throw<TException>(expr);
        }

        public static void ThrowIfNull<TException>([NotNull] object? argument, [CallerArgumentExpression("argument")] string? expr = null)
        {
            if (argument == null)
                Throw<TException>(expr);
        }

        public static void ThrowIfEmpty<TContent, TException>(IEnumerable<TContent> argument, [CallerArgumentExpression("argument")] string? expr = null)
        {
            if (argument.FirstOrDefault() == null)
                Throw<TException>(expr);
        }

        [DoesNotReturn]
        private static void Throw<TException>(string? message)
        {
            throw lookup[typeof(TException)](message!);
        }
    }
}
