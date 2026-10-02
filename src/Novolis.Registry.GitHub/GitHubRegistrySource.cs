using Novolis.Registry.Abstractions;
using Novolis.Registry.Primitives;
using Novolis.Registry;
using Novolis.IO.GitHub;

namespace Novolis.Registry.GitHub;

/// <summary>Loads a registry document from a public GitHub repository file.</summary>
public sealed class GitHubRegistrySource : IRegistrySource
{
    private readonly HttpClient _httpClient;
    private readonly GitHubRegistrySourceOptions _options;

    /// <summary>Creates a source using an existing HTTP client.</summary>
    public GitHubRegistrySource(
        HttpClient httpClient,
        GitHubRegistrySourceOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async ValueTask<RegistryDocument> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var uri = _options.Repository.RawFile(_options.Branch, _options.Path);
        using var response = await _httpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        return await RegistryJson.DeserializeAsync(stream, cancellationToken)
            .ConfigureAwait(false);
    }
}
