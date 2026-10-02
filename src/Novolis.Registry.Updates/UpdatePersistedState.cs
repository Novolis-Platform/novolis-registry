using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>Durable state needed to resume update checks safely.</summary>
public sealed record UpdatePersistedState
{
    /// <summary>Last observable coordinator state.</summary>
    public UpdateState State { get; init; } = UpdateState.Idle;

    /// <summary>Latest candidate identity known to this installation.</summary>
    public string? CandidateIdentity { get; init; }

    /// <summary>Latest candidate needed to render and resume the update prompt.</summary>
    public UpdateReleaseCandidate? Candidate { get; init; }

    /// <summary>Version last shown to the user.</summary>
    public string? LastNotifiedIdentity { get; init; }

    /// <summary>Candidate snooze deadline.</summary>
    public DateTimeOffset? SnoozedUntil { get; init; }

    /// <summary>Source cache and backoff state.</summary>
    public UpdateSourceCheckpoint Checkpoint { get; init; } = new();

    /// <summary>Verified downloaded file path, when one is ready.</summary>
    public string? DownloadPath { get; init; }

    /// <summary>Verified downloaded artifact, when one is ready.</summary>
    public DownloadedUpdateArtifact? Download { get; init; }

    /// <summary>Last stable error code safe to expose to support.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Last stable error message safe to show in the host.</summary>
    public string? ErrorMessage { get; init; }
}
