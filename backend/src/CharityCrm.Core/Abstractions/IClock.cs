namespace CharityCrm.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Today's calendar date in the organization's time zone (Europe/Vienna).</summary>
    DateOnly Today { get; }
}
