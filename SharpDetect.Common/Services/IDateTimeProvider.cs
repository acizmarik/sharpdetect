namespace SharpDetect.Common.Services
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }

    public sealed class UtcDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.UtcNow;
    }
}
