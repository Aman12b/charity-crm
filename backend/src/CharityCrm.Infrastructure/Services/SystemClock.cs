using CharityCrm.Core.Abstractions;

namespace CharityCrm.Infrastructure.Services;

public sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    private static readonly TimeZoneInfo Vienna = TimeZoneInfo.FindSystemTimeZoneById("Europe/Vienna");

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, Vienna).DateTime);
}
