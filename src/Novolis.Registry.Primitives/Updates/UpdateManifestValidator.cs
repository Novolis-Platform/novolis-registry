using System.Text.RegularExpressions;

namespace Novolis.Registry.Primitives.Updates;

/// <summary>Validates standalone update manifests before they are consumed.</summary>
public static partial class UpdateManifestValidator
{
    private static readonly Regex AppIdPattern = AppIdRegex();
    private static readonly Regex Sha256Pattern = Sha256Regex();

    /// <summary>Returns deterministic validation errors without throwing.</summary>
    public static IReadOnlyList<string> Validate(UpdateManifest? manifest)
    {
        var errors = new List<string>();
        if (manifest is null)
        {
            errors.Add("manifest is required");
            return errors;
        }

        if (manifest.SchemaVersion != 1)
            errors.Add("schemaVersion must be 1");
        if (string.IsNullOrWhiteSpace(manifest.AppId) || !AppIdPattern.IsMatch(manifest.AppId))
            errors.Add("appId must contain only letters, digits, '.', '_' or '-'");
        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
            errors.Add("displayName is required");
        if (!UpdateVersionComparer.IsValid(manifest.Version))
            errors.Add("version must be a supported CalVer");
        if (manifest.PublishedAt == default || manifest.PublishedAt.Offset != TimeSpan.Zero)
            errors.Add("publishedAt must be a UTC timestamp");

        ValidateProvenance(manifest, errors);
        if (manifest.MinimumVersion is not null && !UpdateVersionComparer.IsValid(manifest.MinimumVersion))
            errors.Add("minimumVersion must be a supported CalVer");
        if (manifest.MinimumVersion is not null
            && UpdateVersionComparer.IsValid(manifest.MinimumVersion)
            && UpdateVersionComparer.IsValid(manifest.Version)
            && UpdateVersionComparer.Compare(manifest.MinimumVersion, manifest.Version) > 0)
        {
            errors.Add("minimumVersion cannot be newer than version");
        }

        if (manifest.Artifacts is null || manifest.Artifacts.Count == 0)
        {
            errors.Add("artifacts must contain at least one item");
        }
        else
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < manifest.Artifacts.Count; index++)
            {
                var artifact = manifest.Artifacts[index];
                ValidateArtifact(artifact, index, names, identities, errors);
            }
        }

        return errors;
    }

    /// <summary>Validates a manifest and throws one exception containing all failures.</summary>
    public static void ValidateOrThrow(UpdateManifest manifest)
    {
        var errors = Validate(manifest);
        if (errors.Count != 0)
            throw new UpdateManifestValidationException(errors);
    }

    private static void ValidateProvenance(UpdateManifest manifest, List<string> errors)
    {
        if (manifest.Provenance is null)
        {
            errors.Add("provenance is required");
            return;
        }

        if (!string.Equals(manifest.AppId, manifest.Provenance.AppId, StringComparison.Ordinal))
            errors.Add("provenance.appId must match appId");
        ValidateHttps(manifest.Provenance.Repository, "provenance.repository", errors);
        ValidateHttps(manifest.Provenance.ReleaseUri, "provenance.releaseUri", errors);

        if (string.IsNullOrWhiteSpace(manifest.Provenance.Tag))
        {
            errors.Add("provenance.tag is required");
        }
        else if (UpdateVersionComparer.IsValid(manifest.Version)
            && !UpdateVersionComparer.IsValid(manifest.Provenance.Tag))
        {
            errors.Add("provenance.tag must be a version or v-prefixed version");
        }
        else if (UpdateVersionComparer.IsValid(manifest.Version)
            && UpdateVersionComparer.IsValid(manifest.Provenance.Tag)
            && UpdateVersionComparer.Compare(
                manifest.Version,
                manifest.Provenance.Tag.TrimStart('v')) != 0)
        {
            errors.Add("provenance.tag must identify version");
        }
    }

    private static void ValidateArtifact(
        UpdateArtifact? artifact,
        int index,
        HashSet<string> names,
        HashSet<string> identities,
        List<string> errors)
    {
        if (artifact is null)
        {
            errors.Add($"artifacts[{index}] is required");
            return;
        }

        var prefix = $"artifacts[{index}]";
        if (string.IsNullOrWhiteSpace(artifact.Name)
            || artifact.Name.Contains('/', StringComparison.Ordinal)
            || artifact.Name.Contains('\\', StringComparison.Ordinal)
            || artifact.Name.Contains("..", StringComparison.Ordinal))
        {
            errors.Add($"{prefix}.name must be a safe basename");
        }
        else if (!names.Add(artifact.Name))
        {
            errors.Add($"{prefix}.name is duplicated");
        }

        ValidateHttps(artifact.DownloadUri, $"{prefix}.downloadUri", errors);
        if (artifact.Length <= 0)
            errors.Add($"{prefix}.length must be greater than zero");
        if (string.IsNullOrWhiteSpace(artifact.Sha256) || !Sha256Pattern.IsMatch(artifact.Sha256))
            errors.Add($"{prefix}.sha256 must be 64 hexadecimal characters");
        if (artifact.Target is null)
        {
            errors.Add($"{prefix}.target is required");
        }
        else
        {
            if (artifact.Target.Platform != PlatformFor(artifact.Kind))
                errors.Add($"{prefix}.target.platform does not match kind");
            if (artifact.Target.RuntimeIdentifier?.Contains('/', StringComparison.Ordinal) == true
                || artifact.Target.RuntimeIdentifier?.Contains('\\', StringComparison.Ordinal) == true)
            {
                errors.Add($"{prefix}.target.runtimeIdentifier is invalid");
            }

            var identity =
                $"{artifact.Kind}|{artifact.Target.Platform}|{artifact.Target.RuntimeIdentifier}|{artifact.Target.Architecture}";
            if (!identities.Add(identity))
                errors.Add($"{prefix} duplicates another target");
        }
    }

    private static UpdatePlatform PlatformFor(UpdateArtifactKind kind) => kind switch
    {
        UpdateArtifactKind.WindowsInstaller or UpdateArtifactKind.WindowsPortable =>
            UpdatePlatform.Windows,
        UpdateArtifactKind.LinuxTarGz => UpdatePlatform.Linux,
        UpdateArtifactKind.AndroidApk => UpdatePlatform.Android,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static void ValidateHttps(Uri? uri, string name, List<string> errors)
    {
        if (uri is null || !uri.IsAbsoluteUri || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            errors.Add($"{name} must be an absolute HTTPS URI");
    }

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{1,127}$")]
    private static partial Regex AppIdRegex();

    [GeneratedRegex("^[0-9a-fA-F]{64}$")]
    private static partial Regex Sha256Regex();
}
