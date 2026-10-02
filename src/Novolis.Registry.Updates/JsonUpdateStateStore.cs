using System.Text.Json;

namespace Novolis.Registry.Updates;

/// <summary>Atomic JSON-file state store suitable for per-user app data.</summary>
public sealed class JsonUpdateStateStore : IUpdateStateStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string _path;

    /// <summary>Creates a store at an application-owned path.</summary>
    public JsonUpdateStateStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A state path is required.", nameof(path));
        _path = Path.GetFullPath(path);
    }

    /// <inheritdoc />
    public async ValueTask<UpdatePersistedState?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
            return null;

        try
        {
            await using var stream = new FileStream(
                _path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<UpdatePersistedState>(
                    stream,
                    Options,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new UpdateSourceException(
                "state-corrupt",
                "The saved update state is corrupt.",
                exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(
        UpdatePersistedState state,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var directory = Path.GetDirectoryName(_path);
        if (directory is null)
            throw new IOException("The update state path has no directory.");
        Directory.CreateDirectory(directory);

        var temporary = $"{_path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await JsonSerializer.SerializeAsync(stream, state, Options, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }
}
