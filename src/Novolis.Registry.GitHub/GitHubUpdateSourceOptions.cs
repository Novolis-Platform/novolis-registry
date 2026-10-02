using Novolis.IO.GitHub;

namespace Novolis.Registry.GitHub;

/// <summary>Transport and release-selection options for GitHub update checks.</summary>
public sealed record GitHubUpdateSourceOptions
{
    /// <summary>Repository whose releases are the app provenance source.</summary>
    public required GitHubRepository Repository { get; init; }

    /// <summary>Name of the standalone release metadata asset.</summary>
    public string ManifestAssetName { get; init; } = "Novolis.Update.json";

    /// <summary>Maximum number of release records to inspect.</summary>
    public int MaximumReleases { get; init; } = 20;

    /// <summary>Maximum bytes accepted for a release list or manifest.</summary>
    public long MaximumResponseBytes { get; init; } = 4 * 1024 * 1024;

    /// <summary>HTTP user-agent required by the GitHub API.</summary>
    public string UserAgent { get; init; } = "Novolis-Registry-Updates";

    /// <summary>Validates options before a source starts polling.</summary>
    public void Validate()
    {
        if (Repository is null)
            throw new ArgumentException("A GitHub repository is required.", nameof(Repository));
        if (string.IsNullOrWhiteSpace(ManifestAssetName)
            || ManifestAssetName.Contains('/', StringComparison.Ordinal)
            || ManifestAssetName.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("The manifest asset name must be a basename.", nameof(ManifestAssetName));
        }
        if (MaximumReleases is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(MaximumReleases));
        if (MaximumResponseBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaximumResponseBytes));
        if (string.IsNullOrWhiteSpace(UserAgent))
            throw new ArgumentException("A user-agent is required.", nameof(UserAgent));
    }
}
