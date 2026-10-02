namespace Novolis.Registry.Updates;

/// <summary>Host-provided platform handoff for a verified downloaded artifact.</summary>
public interface IPlatformUpdateAction
{
    /// <summary>Whether this host can hand off the artifact.</summary>
    bool CanApply { get; }

    /// <summary>Hands the artifact to an installer or package installer.</summary>
    ValueTask ApplyAsync(
        DownloadedUpdateArtifact artifact,
        CancellationToken cancellationToken = default);
}
