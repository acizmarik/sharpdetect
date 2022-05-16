namespace SharpDetect.Common.Services.Instrumentation
{
    public interface IInstrumentor
    {
        int InstrumentedMethodsCount { get; }
        int InjectedMethodWrappersCount { get; }
        int InjectedMethodHooksCount { get; }
    }
}
