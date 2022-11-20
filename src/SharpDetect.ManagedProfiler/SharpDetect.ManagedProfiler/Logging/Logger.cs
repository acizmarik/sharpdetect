using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SharpDetect.Profiler.Logging
{
    internal static class Logger
    {
        private static ImmutableArray<ILoggerSink> sinks;
        private static LogLevel logLevel;
        private const string defaultLogErrorMessageFormat = "{0} {1} at {2}:{3}";
        private const string defaultLogInfoMessageFormat = "{0} {1}";

        static Logger()
        {
            // Initialize defaults for logging
            sinks = new[] { (ILoggerSink)new ConsoleSink() }.ToImmutableArray();
            logLevel = LogLevel.Debug;
        }

        public static void Initialize(LogLevel level = LogLevel.Debug, params ILoggerSink[] sinks)
        {
            logLevel = level;

            var builder = ImmutableArray.CreateBuilder<ILoggerSink>();
            foreach (var sink in sinks)
                builder.Add(sink);

            Logger.sinks = builder.ToImmutableArray();
        }

        public static void Terminate()
        {
            foreach (var sink in sinks)
                sink.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string message, [CallerFilePath] string? filePath = null, [CallerLineNumber] int? lineNumber = null)
            => Log(LogLevel.Error, defaultLogErrorMessageFormat, message, filePath, lineNumber);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(string message, [CallerFilePath] string? filePath = null, [CallerLineNumber] int? lineNumber = null)
            => Log(LogLevel.Warning, defaultLogErrorMessageFormat, message, filePath, lineNumber);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogInformation(string message, [CallerFilePath] string? filePath = null, [CallerLineNumber] int? lineNumber = null)
            => Log(LogLevel.Information, defaultLogInfoMessageFormat, message, filePath, lineNumber);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug(string message, [CallerFilePath] string? filePath = null, [CallerLineNumber] int? lineNumber = null)
            => Log(LogLevel.Debug, defaultLogInfoMessageFormat, message, filePath, lineNumber);

        private static void Log(LogLevel level, string format, string message, string? filePath, int? lineNumber)
        {
            if (level < logLevel)
                return;

            var levelString = level switch
            {
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                _ => "???"
            };

            var formattedMessage = string.Format(format, $"[{levelString}]", message, filePath, lineNumber);
            foreach (var sink in sinks)
                sink.WriteLine(formattedMessage);
        }
    }
}
