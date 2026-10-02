using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Registry.Primitives.Updates;

/// <summary>Canonical JSON serialization for <c>Novolis.Update.json</c>.</summary>
public static class UpdateManifestJson
{
    /// <summary>Stable web-style options used for release assets.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Serializes and validates a manifest.</summary>
    public static string Serialize(UpdateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        UpdateManifestValidator.ValidateOrThrow(manifest);
        return JsonSerializer.Serialize(manifest, Options);
    }

    /// <summary>Serializes and validates a release-level catalog.</summary>
    public static string SerializeCatalog(UpdateManifestCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        UpdateManifestCatalogValidator.ValidateOrThrow(catalog);
        return JsonSerializer.Serialize(catalog, Options);
    }

    /// <summary>Deserializes and validates a manifest from UTF-8 JSON.</summary>
    public static UpdateManifest Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var manifest = JsonSerializer.Deserialize<UpdateManifest>(json, Options)
            ?? throw new JsonException("The update manifest was empty.");
        UpdateManifestValidator.ValidateOrThrow(manifest);
        return manifest;
    }

    /// <summary>Deserializes and validates a manifest from a stream.</summary>
    public static async ValueTask<UpdateManifest> DeserializeAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(
                stream,
                Options,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new JsonException("The update manifest was empty.");
        UpdateManifestValidator.ValidateOrThrow(manifest);
        return manifest;
    }

    /// <summary>Deserializes either a catalog or a legacy single-manifest asset.</summary>
    public static UpdateManifestCatalog DeserializeCatalog(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("updates", out _))
        {
            var catalog = JsonSerializer.Deserialize<UpdateManifestCatalog>(json, Options)
                ?? throw new JsonException("The update catalog was empty.");
            UpdateManifestCatalogValidator.ValidateOrThrow(catalog);
            return catalog;
        }

        var manifest = Deserialize(json);
        return new UpdateManifestCatalog
        {
            GeneratedAt = manifest.PublishedAt,
            Updates = [manifest],
        };
    }

    /// <summary>Returns canonical UTF-8 bytes for a release-level catalog.</summary>
    public static byte[] SerializeCatalogUtf8(UpdateManifestCatalog catalog) =>
        Encoding.UTF8.GetBytes(SerializeCatalog(catalog));

    /// <summary>Returns canonical UTF-8 bytes for hashing or publishing.</summary>
    public static byte[] SerializeUtf8(UpdateManifest manifest) =>
        Encoding.UTF8.GetBytes(Serialize(manifest));

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
