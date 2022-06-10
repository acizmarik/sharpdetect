using SharpDetect.Common.Instrumentation;

namespace SharpDetect.Instrumentation
{
    internal class InstrumentationOptions
    {
        public bool Enabled { get; private set; }
        public InstrumentationStrategy Strategy { get; private set; }
        public string[] Patterns { get; private set; }

        public InstrumentationOptions(bool enabled, InstrumentationStrategy strategy, string[]? patterns)
        {
            Enabled = enabled;
            Strategy = strategy;
            Patterns = patterns ?? Array.Empty<string>();
        }
    }
}
