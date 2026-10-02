using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>Result of a conditional update-source check.</summary>
public sealed record UpdateSourceResult
{
    /// <summary>Eligible candidate, or null when no newer update exists.</summary>
    public UpdateReleaseCandidate? Candidate { get; init; }

    /// <summary>True when the provider returned HTTP 304 or equivalent.</summary>
    public bool NotModified { get; init; }

    /// <summary>Entity tag to use for the next check.</summary>
    public string? ETag { get; init; }

    /// <summary>Optional provider-directed backoff time.</summary>
    public DateTimeOffset? RetryAfter { get; init; }
}
