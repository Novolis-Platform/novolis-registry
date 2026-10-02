namespace Novolis.Registry.Primitives.Updates;

/// <summary>Validates a release-level update catalog.</summary>
public static class UpdateManifestCatalogValidator
{
    /// <summary>Returns deterministic catalog validation errors.</summary>
    public static IReadOnlyList<string> Validate(UpdateManifestCatalog? catalog)
    {
        var errors = new List<string>();
        if (catalog is null)
        {
            errors.Add("catalog is required");
            return errors;
        }

        if (catalog.SchemaVersion != 1)
            errors.Add("schemaVersion must be 1");
        if (catalog.GeneratedAt == default || catalog.GeneratedAt.Offset != TimeSpan.Zero)
            errors.Add("generatedAt must be a UTC timestamp");
        if (catalog.Updates.Count == 0)
        {
            errors.Add("updates must contain at least one manifest");
            return errors;
        }

        var appIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var manifest in catalog.Updates)
        {
            foreach (var error in UpdateManifestValidator.Validate(manifest))
                errors.Add($"{manifest?.AppId ?? "<missing>"}: {error}");
            if (manifest is not null && !appIds.Add(manifest.AppId))
                errors.Add($"duplicate appId '{manifest.AppId}'");
        }

        return errors;
    }

    /// <summary>Validates a catalog and throws all failures together.</summary>
    public static void ValidateOrThrow(UpdateManifestCatalog catalog)
    {
        var errors = Validate(catalog);
        if (errors.Count != 0)
            throw new UpdateManifestValidationException(errors);
    }
}
