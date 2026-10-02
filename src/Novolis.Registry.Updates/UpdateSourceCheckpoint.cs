namespace Novolis.Registry.Updates;

/// <summary>Conditional-request and bounded-backoff state for a source.</summary>
public sealed record UpdateSourceCheckpoint
{
    /// <summary>Last entity tag returned by the source.</summary>
    public string? ETag { get; init; }

    /// <summary>Last completed source check.</summary>
    public DateTimeOffset? LastCheckedAt { get; init; }

    /// <summary>Do not query the source before this time.</summary>
    public DateTimeOffset? RetryAfter { get; init; }
}
