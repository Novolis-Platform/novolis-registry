namespace Novolis.Registry.Primitives.Updates;

/// <summary>Raised when a standalone update manifest violates its contract.</summary>
public sealed class UpdateManifestValidationException : FormatException
{
    /// <summary>Creates an exception containing all validation failures.</summary>
    public UpdateManifestValidationException(IReadOnlyList<string> errors)
        : base($"The update manifest is invalid: {string.Join("; ", errors)}")
    {
        Errors = errors;
    }

    /// <summary>Individual deterministic validation messages.</summary>
    public IReadOnlyList<string> Errors { get; }
}
