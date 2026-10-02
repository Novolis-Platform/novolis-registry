namespace Novolis.Registry.Updates;

/// <summary>Polling, backoff, and notification policy for an update coordinator.</summary>
public sealed record UpdateCoordinatorOptions
{
    /// <summary>Minimum interval between source requests.</summary>
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromHours(6);

    /// <summary>Default user snooze duration.</summary>
    public TimeSpan SnoozeDuration { get; init; } = TimeSpan.FromDays(1);

    /// <summary>Maximum provider-directed retry delay accepted by the coordinator.</summary>
    public TimeSpan MaximumRetryDelay { get; init; } = TimeSpan.FromDays(1);

    /// <summary>Validates policy values.</summary>
    public void Validate()
    {
        if (CheckInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(CheckInterval));
        if (SnoozeDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(SnoozeDuration));
        if (MaximumRetryDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(MaximumRetryDelay));
    }
}
