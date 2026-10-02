using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>Immutable projection consumed by Avalonia, MAUI, and headless hosts.</summary>
public sealed record UpdateSnapshot
{
    /// <summary>Current coordinator state.</summary>
    public UpdateState State { get; init; } = UpdateState.Idle;

    /// <summary>Installed version supplied by the host.</summary>
    public required string CurrentVersion { get; init; }

    /// <summary>Candidate selected for this target, when available.</summary>
    public UpdateReleaseCandidate? Candidate { get; init; }

    /// <summary>Download progress, when downloading.</summary>
    public UpdateDownloadProgress? Progress { get; init; }

    /// <summary>Verified local artifact, when ready.</summary>
    public DownloadedUpdateArtifact? Download { get; init; }

    /// <summary>Safe error code for diagnostics.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Safe user-facing error text.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Whether the candidate should produce a new notification.</summary>
    public bool ShouldNotify { get; init; }

    /// <summary>Whether the current candidate is mandatory.</summary>
    public bool IsMandatory => Candidate?.Manifest.Mandatory == true;
}
