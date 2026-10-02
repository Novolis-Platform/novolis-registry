namespace Novolis.Registry.Primitives.Updates;

/// <summary>Immutable release provenance used to identify an update source.</summary>
public sealed record UpdateProvenance
{
    /// <summary>Stable application identifier, never a display name.</summary>
    public required string AppId { get; init; }

    /// <summary>Canonical GitHub repository that published the release.</summary>
    public required Uri Repository { get; init; }

    /// <summary>Git tag associated with the release.</summary>
    public required string Tag { get; init; }

    /// <summary>Public release page for human review.</summary>
    public required Uri ReleaseUri { get; init; }

    /// <summary>Optional commit SHA recorded by the release pipeline.</summary>
    public string? Commit { get; init; }
}
