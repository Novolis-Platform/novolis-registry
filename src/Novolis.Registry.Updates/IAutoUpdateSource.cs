using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Updates;

/// <summary>Provider-neutral source of the latest eligible update.</summary>
public interface IAutoUpdateSource
{
    /// <summary>Checks a release source using the supplied cache checkpoint.</summary>
    ValueTask<UpdateSourceResult> CheckAsync(
        UpdateRequest request,
        UpdateSourceCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
}
