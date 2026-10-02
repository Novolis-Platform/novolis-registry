namespace Novolis.Registry.Primitives.Updates;

/// <summary>Specific operating-system and runtime target of an artifact.</summary>
public sealed record UpdateTarget
{
    /// <summary>Operating system represented by the target.</summary>
    public required UpdatePlatform Platform { get; init; }

    /// <summary>Optional .NET runtime identifier, such as <c>win-x64</c>.</summary>
    public string? RuntimeIdentifier { get; init; }

    /// <summary>Optional architecture identifier, such as <c>x64</c> or <c>arm64</c>.</summary>
    public string? Architecture { get; init; }
}
