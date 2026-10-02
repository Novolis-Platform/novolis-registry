using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Novolis.IO.GitHub;
using Novolis.Registry.GitHub;
using Novolis.Registry.Primitives;
using Novolis.Registry.Primitives.Updates;
using Novolis.Registry.Updates;

namespace Novolis.Registry.Unit;

public sealed class GitHubUpdateSourceTests
{
    [Test]
    public async Task Source_selects_by_app_channel_target_and_manifest_asset()
    {
        var manifest = UpdateManifestContractTests.CreateManifest();
        var handler = new FakeGitHubHandler(
            ReleaseList(v: "2026.1.2.0"),
            UpdateManifestJson.SerializeCatalog(new UpdateManifestCatalog
            {
                GeneratedAt = UpdateManifestContractTests.Utc("2026-10-03T00:00:00Z"),
                Updates = [manifest],
            }));
        using var client = new HttpClient(handler);
        var source = CreateSource(client);

        var result = await source.CheckAsync(Request(), new UpdateSourceCheckpoint());

        await Assert.That(result.Candidate).IsNotNull();
        await Assert.That(result.Candidate!.Artifact.Name)
            .IsEqualTo("Example-2026.1.2.0-win-x64.zip");
        await Assert.That(handler.Requests).Count().IsEqualTo(2);
        await Assert.That(handler.Requests[0].Headers.UserAgent.ToString())
            .Contains("Novolis-Registry-Updates");
    }

    [Test]
    public async Task Source_honors_etag_304_without_fetching_a_manifest()
    {
        var handler = new FakeGitHubHandler(string.Empty, null)
        {
            ReleaseStatus = HttpStatusCode.NotModified,
        };
        using var client = new HttpClient(handler);
        var source = CreateSource(client);

        var result = await source.CheckAsync(
            Request(),
            new UpdateSourceCheckpoint { ETag = "\"cached\"" });

        await Assert.That(result.NotModified).IsTrue();
        await Assert.That(result.ETag).IsEqualTo("\"cached\"");
        await Assert.That(handler.Requests).Count().IsEqualTo(1);
        await Assert.That(handler.Requests[0].Headers.GetValues("If-None-Match").Single())
            .IsEqualTo("\"cached\"");
    }

    [Test]
    public async Task Source_rejects_store_managed_requests_before_network()
    {
        var handler = new FakeGitHubHandler(string.Empty, null);
        using var client = new HttpClient(handler);
        var source = CreateSource(client);

        await Assert.That(async () => await source.CheckAsync(
                Request() with { DistributionMode = UpdateDistributionMode.StoreManaged },
                new UpdateSourceCheckpoint()))
            .Throws<UpdateSourceException>();
        await Assert.That(handler.Requests).IsEmpty();
    }

    [Test]
    public async Task Source_surfaces_rate_limit_with_retry_time()
    {
        var handler = new FakeGitHubHandler(string.Empty, null)
        {
            ReleaseStatus = HttpStatusCode.Forbidden,
            RetryAfter = "30",
        };
        using var client = new HttpClient(handler);
        var source = CreateSource(client);

        UpdateSourceException? exception = null;
        try
        {
            await source.CheckAsync(Request(), new UpdateSourceCheckpoint());
        }
        catch (UpdateSourceException caught)
        {
            exception = caught;
        }

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception!.Code).IsEqualTo("rate-limited");
        await Assert.That(exception.RetryAfter).IsNotNull();
    }

    private static GitHubUpdateSource CreateSource(HttpClient client) =>
        new(
            client,
            new GitHubUpdateSourceOptions
            {
                Repository = new GitHubRepository
                {
                    Owner = "Novolis-Platform",
                    Name = "example",
                },
            });

    private static UpdateRequest Request() =>
        new()
        {
            AppId = "Novolis.Example",
            Repository = new Uri("https://github.com/Novolis-Platform/example"),
            CurrentVersion = "2026.1.1.0",
            Platform = UpdatePlatform.Windows,
            RuntimeIdentifier = "win-x64",
            Architecture = "x64",
            ArtifactKind = UpdateArtifactKind.WindowsPortable,
        };

    private static string ReleaseList(string v) =>
        JsonSerializer.Serialize(new[]
        {
            new
            {
                draft = false,
                prerelease = false,
                tag_name = $"v{v}",
                html_url = $"https://github.com/Novolis-Platform/example/releases/tag/v{v}",
                assets = new[]
                {
                    new
                    {
                        name = "Novolis.Update.json",
                        browser_download_url = $"https://github.com/Novolis-Platform/example/releases/download/v{v}/Novolis.Update.json",
                    },
                },
            },
        });

    private sealed class FakeGitHubHandler(
        string releaseList,
        string? manifest) : HttpMessageHandler
    {
        private readonly string _releaseList = releaseList;
        private readonly string? _manifest = manifest;

        public List<HttpRequestMessage> Requests { get; } = [];
        public HttpStatusCode ReleaseStatus { get; init; } = HttpStatusCode.OK;
        public string? RetryAfter { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            if (request.RequestUri!.AbsolutePath.EndsWith("/releases", StringComparison.Ordinal))
            {
                var response = new HttpResponseMessage(ReleaseStatus)
                {
                    Content = new StringContent(
                        ReleaseStatus == HttpStatusCode.OK ? _releaseList : "",
                        Encoding.UTF8,
                        "application/json"),
                };
                response.Headers.ETag = new EntityTagHeaderValue("\"cached\"");
                if (RetryAfter is not null)
                    response.Headers.TryAddWithoutValidation("Retry-After", RetryAfter);
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    _manifest ?? "",
                    Encoding.UTF8,
                    "application/json"),
            });
        }
    }
}
