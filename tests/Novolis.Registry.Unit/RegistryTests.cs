using System.Text;
using Novolis.Registry.Primitives;

namespace Novolis.Registry.Unit;

public sealed class RegistryTests
{
    [Test]
    public async Task Json_round_trips_catalog_entries()
    {
        var document = new RegistryDocument
        {
            GeneratedAt = DateTimeOffset.Parse("2026-10-02T00:00:00Z"),
            Entries =
            [
                new RegistryEntry
                {
                    Id = "example-app",
                    Name = "Example App",
                    Kind = RegistryItemKind.Application,
                    Version = "2026.1.0.1",
                    Artifacts =
                    [
                        new RegistryArtifact
                        {
                            Name = "ExampleSetup.exe",
                            Platform = RegistryPlatform.Windows,
                            Kind = "installer",
                            DownloadUri = new Uri("https://example.test/ExampleSetup.exe"),
                            Sha256 = "abc",
                        },
                    ],
                },
            ],
        };

        var json = Novolis.Registry.RegistryJson.Serialize(document);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var roundTrip = await Novolis.Registry.RegistryJson.DeserializeAsync(stream);

        await Assert.That(roundTrip.SchemaVersion).IsEqualTo(1);
        await Assert.That(roundTrip.Entries.Count).IsEqualTo(1);
        await Assert.That(roundTrip.Entries[0].Artifacts[0].Platform)
            .IsEqualTo(RegistryPlatform.Windows);
    }

    [Test]
    public async Task Service_finds_entry_case_insensitively()
    {
        var document = new RegistryDocument
        {
            Entries =
            [
                new RegistryEntry
                {
                    Id = "example-app",
                    Name = "Example App",
                    Kind = RegistryItemKind.Application,
                    Version = "2026.1.0.1",
                },
            ],
        };

        var service = new Novolis.Registry.RegistryService(new TestRegistrySource(document));
        var entry = await service.FindAsync("EXAMPLE-APP");

        await Assert.That(entry).IsNotNull();
        await Assert.That(entry!.Name).IsEqualTo("Example App");
    }
}
