using System.Text.Json;
using System.Text.Json.Serialization;
using Novolis.Registry.Primitives;

namespace Novolis.Registry;

/// <summary>JSON serialization helpers for registry documents.</summary>
public static class RegistryJson
{
    /// <summary>Canonical web-style options used by registry documents.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>Deserializes a registry document from a stream.</summary>
    public static async ValueTask<RegistryDocument> DeserializeAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var document = await JsonSerializer.DeserializeAsync<RegistryDocument>(
            stream,
            Options,
            cancellationToken).ConfigureAwait(false);

        return document ?? throw new JsonException("The registry document was empty.");
    }

    /// <summary>Serializes a registry document to UTF-8 JSON.</summary>
    public static string Serialize(RegistryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return JsonSerializer.Serialize(document, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
