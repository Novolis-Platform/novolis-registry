using Novolis.IO.GitHub;

namespace Novolis.Registry.GitHub;

/// <summary>Location of a registry document stored in GitHub raw content.</summary>
public sealed record GitHubRegistrySourceOptions
{
    /// <summary>Repository containing the registry document.</summary>
    public required GitHubRepository Repository { get; init; }

    /// <summary>Branch containing the document.</summary>
    public string Branch { get; init; } = "main";

    /// <summary>Repository-relative path to the JSON document.</summary>
    public string Path { get; init; } = "index.json";
}
