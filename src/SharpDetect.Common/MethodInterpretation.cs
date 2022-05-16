namespace SharpDetect.Common
{
    public enum MethodInterpretation
    {
        Regular,

        FieldAccess,
        FieldInstanceAccess,

        ArrayIndexAccess,
        ArrayInstanceAccess,
        ArrayElementAccess,

        LockBlockingAcquire,
        LockTryAcquire,
        LockRelease,

        SignalBlockingWait,
        SignalTryWait,
        SignalPulseOne,
        SignalPulseAll
    }
}
