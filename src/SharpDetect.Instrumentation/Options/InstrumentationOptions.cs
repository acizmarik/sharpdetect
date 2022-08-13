using SharpDetect.Common.Instrumentation;

namespace SharpDetect.Instrumentation.Options
{
    internal record struct InstrumentationOptions(RewritingOptions RewritingOptions, EntryExitHookOptions HookOptions);
}
