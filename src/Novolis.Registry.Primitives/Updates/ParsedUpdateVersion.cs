namespace Novolis.Registry.Primitives.Updates;

/// <summary>Parsed components of a supported update version.</summary>
public sealed record ParsedUpdateVersion(
    IReadOnlyList<int> Numbers,
    string? PreRelease);
