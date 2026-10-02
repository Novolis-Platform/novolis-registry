using Novolis.Registry.Primitives;
using Novolis.Registry.Primitives.Updates;

namespace Novolis.Registry.Unit;

public sealed class UpdateManifestContractTests
{
    [Test]
    public async Task Manifest_round_trip_is_deterministic_and_uses_camel_case_enums()
    {
        var manifest = CreateManifest();
        var first = UpdateManifestJson.Serialize(manifest);
        var second = UpdateManifestJson.Serialize(UpdateManifestJson.Deserialize(first));

        await Assert.That(second).IsEqualTo(first);
        await Assert.That(first).Contains("\"distributionMode\": \"directGithub\"");
        await Assert.That(first).Contains("\"windowsPortable\"");
    }

    [Test]
    public async Task Catalog_round_trip_contains_one_manifest_per_app()
    {
        var catalog = new UpdateManifestCatalog
        {
            GeneratedAt = Utc("2026-10-03T00:00:00Z"),
            Updates = [CreateManifest(), CreateManifest("Novolis.Other", "2026.1.3.0")],
        };

        var json = UpdateManifestJson.SerializeCatalog(catalog);
        var result = UpdateManifestJson.DeserializeCatalog(json);

        await Assert.That(result.Updates).Count().IsEqualTo(2);
        await Assert.That(result.Updates[1].AppId).IsEqualTo("Novolis.Other");
    }

    [Test]
    public async Task Validator_rejects_duplicate_assets_invalid_urls_and_downgrades()
    {
        var manifest = CreateManifest("Novolis.Example", "2026.1.2.0") with
        {
            MinimumVersion = "2026.1.3.0",
            Artifacts =
            [
                CreateArtifact("same.zip"),
                CreateArtifact("same.zip") with
                {
                    DownloadUri = new Uri("http://example.test/same.zip"),
                },
            ],
        };

        var errors = UpdateManifestValidator.Validate(manifest);

        await Assert.That(errors).Contains("minimumVersion cannot be newer than version");
        await Assert.That(errors).Contains("artifacts[1].name is duplicated");
        await Assert.That(errors).Contains("artifacts[1].downloadUri must be an absolute HTTPS URI");
    }

    [Test]
    public async Task Version_policy_orders_calver_and_prerelease_without_allowing_malformed_values()
    {
        await Assert.That(UpdateVersionComparer.Compare("2026.1.2.0", "2026.1.2.0-preview.1"))
            .IsGreaterThan(0);
        await Assert.That(UpdateVersionComparer.Compare("2026.1.2.0-preview.2", "2026.1.2.0-preview.10"))
            .IsLessThan(0);
        await Assert.That(UpdateVersionComparer.IsValid("v2026.13.1.0")).IsFalse();
        await Assert.That(UpdateVersionComparer.IsValid("2026.1")).IsFalse();
    }

    internal static UpdateManifest CreateManifest(
        string appId = "Novolis.Example",
        string version = "2026.1.2.0") =>
        new()
        {
            AppId = appId,
            DisplayName = "Example",
            DistributionMode = UpdateDistributionMode.DirectGithub,
            Channel = RegistryChannel.Stable,
            Version = version,
            PublishedAt = Utc("2026-10-03T00:00:00Z"),
            Provenance = new UpdateProvenance
            {
                AppId = appId,
                Repository = new Uri("https://github.com/Novolis-Platform/example"),
                Tag = $"v{version}",
                ReleaseUri = new Uri($"https://github.com/Novolis-Platform/example/releases/tag/v{version}"),
            },
            Artifacts = [CreateArtifact()],
        };

    internal static UpdateArtifact CreateArtifact(string name = "Example-2026.1.2.0-win-x64.zip") =>
        new()
        {
            Name = name,
            Kind = UpdateArtifactKind.WindowsPortable,
            Target = new UpdateTarget
            {
                Platform = UpdatePlatform.Windows,
                RuntimeIdentifier = "win-x64",
                Architecture = "x64",
            },
            DownloadUri = new Uri($"https://github.com/Novolis-Platform/example/releases/download/v2026.1.2.0/{name}"),
            Length = 42,
            Sha256 = new string('a', 64),
            ContentType = "application/zip",
        };

    internal static DateTimeOffset Utc(string value) => DateTimeOffset.Parse(value).ToUniversalTime();
}
