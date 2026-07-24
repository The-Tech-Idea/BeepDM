using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Installer;
using TheTechIdea.Beep.Updates;
using Xunit;

namespace TheTechIdea.Beep.Updates.Tests;

/// <summary>
/// Stage 11.A coverage — the feed contracts, the async hash-verifying feed client, the
/// version-comparison logic in CheckAsync, and the DI registration. All offline via a fake
/// transport; no network, no filesystem beyond a temp file for the hash test.
/// </summary>
public class UpdatesTests
{
    /// <summary>Serves canned feed text / artifact bytes so the client never touches the network.</summary>
    private sealed class FakeTransport : IFeedTransport
    {
        private readonly string? _text;
        private readonly byte[]? _bytes;
        public FakeTransport(string? text = null, byte[]? bytes = null) { _text = text; _bytes = bytes; }
        public Task<string> GetStringAsync(string url, CancellationToken ct = default)
            => _text != null ? Task.FromResult(_text) : throw new IOException("no text configured");
        public Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
            => _bytes != null ? Task.FromResult(_bytes) : throw new IOException("no bytes configured");
    }

    // ── Feed round-trip ──

    [Fact]
    public async Task Feed_RoundTrips_ThroughSerializeAndFetch()
    {
        var feed = new UpdateFeed
        {
            Product = "MyApp",
            Channel = "stable",
            Latest = new AppReleaseInfo
            {
                Version = "1.2.3",
                MinSupportedVersion = "1.1.0",
                Full = new ArtifactRef { Url = "https://h/Setup-1.2.3.exe", Sha256 = "ABC123" },
                Delta = new DeltaRef { ManifestUrl = "https://h/1.2.3/_payload-manifest.json", BlobBaseUrl = "https://h/1.2.3/_blobs/" }
            }
        };
        feed.Modules.Add(new ModuleRef { Id = "TheTechIdea.Beep.PostgresDriver", Version = "2.4.1", Sha256 = "DEF", Required = false });

        var json = UpdateFeedClient.Serialize(feed);
        var client = new UpdateFeedClient(new FakeTransport(text: json));

        var parsed = await client.FetchFeedAsync("feed.json");

        Assert.Equal("MyApp", parsed.Product);
        Assert.Equal("1.2.3", parsed.Latest!.Version);
        Assert.Equal("1.1.0", parsed.Latest.MinSupportedVersion);
        Assert.Equal("ABC123", parsed.Latest.Full!.Sha256);
        Assert.Equal("https://h/1.2.3/_blobs/", parsed.Latest.Delta!.BlobBaseUrl);
        Assert.Single(parsed.Modules);
        Assert.Equal("TheTechIdea.Beep.PostgresDriver", parsed.Modules[0].Id);
    }

    [Fact]
    public async Task DesignExample_Feed_Parses()
    {
        // The exact shape from the design doc §1.
        const string json = """
        {
          "product": "MyApp",
          "channel": "stable",
          "latest": {
            "version": "1.2.3",
            "releasedAt": "2026-07-23T00:00:00Z",
            "minSupportedVersion": "1.1.0",
            "releaseNotes": "https://x/notes/1.2.3",
            "full":  { "url": "https://x/Setup-MyApp-1.2.3.exe", "sha256": "aaa" },
            "delta": { "manifestUrl": "https://x/1.2.3/_payload-manifest.json", "blobBaseUrl": "https://x/1.2.3/_blobs/" }
          },
          "modules": [
            { "id": "TheTechIdea.Beep.PostgresDriver", "version": "2.4.1", "feed": "https://x/nuget/v3/index.json", "sha256": "bbb", "required": false }
          ]
        }
        """;
        var client = new UpdateFeedClient(new FakeTransport(text: json));

        var feed = await client.FetchFeedAsync("feed.json");

        Assert.Equal("MyApp", feed.Product);
        Assert.Equal("1.2.3", feed.Latest!.Version);
        Assert.Equal(new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero), feed.Latest.ReleasedAt);
        Assert.Equal("https://x/1.2.3/_payload-manifest.json", feed.Latest.Delta!.ManifestUrl);
        Assert.Single(feed.Modules);
        Assert.Equal("2.4.1", feed.Modules[0].Version);
    }

    [Fact]
    public async Task MalformedFeed_ThrowsNamedError()
    {
        var client = new UpdateFeedClient(new FakeTransport(text: "{ this is not json "));

        var ex = await Assert.ThrowsAsync<UpdateFeedException>(() => client.FetchFeedAsync("feed.json"));
        Assert.Contains("not valid JSON", ex.Message);
    }

    // ── Artifact hash verification ──

    [Fact]
    public async Task DownloadVerified_Succeeds_OnMatchingHash()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("payload-v1");
        var sha = Convert.ToHexString(SHA256.HashData(bytes));
        var client = new UpdateFeedClient(new FakeTransport(bytes: bytes));
        var dest = Path.Combine(Path.GetTempPath(), $"beepupd_{Guid.NewGuid():N}.bin");

        try
        {
            var result = await client.DownloadVerifiedAsync("https://h/a.bin", sha, dest);
            Assert.Equal(Errors.Ok, result.Flag);
            Assert.True(File.Exists(dest));
        }
        finally { try { File.Delete(dest); } catch { } }
    }

    [Fact]
    public async Task CorruptArtifact_FailsHashVerification_AndIsDiscarded()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes("tampered");
        var client = new UpdateFeedClient(new FakeTransport(bytes: bytes));
        var dest = Path.Combine(Path.GetTempPath(), $"beepupd_{Guid.NewGuid():N}.bin");

        var result = await client.DownloadVerifiedAsync("https://h/a.bin", "DEADBEEF", dest);

        Assert.Equal(Errors.Failed, result.Flag);
        Assert.Contains("SHA-256", result.Message);
        Assert.False(File.Exists(dest), "a hash-mismatched download must not be left on disk");
    }

    // ── CheckAsync decisions ──

    private static AppUpdateService MakeService(string current, UpdateFeed feed)
        => new(new UpdateSettings { FeedUrl = "feed.json", CurrentVersion = current },
               new UpdateFeedClient(new FakeTransport(text: UpdateFeedClient.Serialize(feed))));

    [Fact]
    public async Task Check_ReportsUpdate_AndRaisesEvent_WhenFeedIsNewer()
    {
        var svc = MakeService("1.0.0", new UpdateFeed { Product = "P", Latest = new AppReleaseInfo { Version = "2.0.0" } });
        UpdateCheckResult? raised = null;
        svc.UpdateAvailable += (_, r) => raised = r;

        var res = await svc.CheckAsync();

        Assert.True(res.Succeeded);
        Assert.True(res.AppUpdateAvailable);
        Assert.Equal("2.0.0", res.LatestVersion);
        Assert.False(res.RequiresFullInstall);
        Assert.NotNull(raised);
    }

    [Fact]
    public async Task Check_FlagsFullInstall_WhenBelowMinSupported()
    {
        var svc = MakeService("1.0.0", new UpdateFeed
        {
            Product = "P",
            Latest = new AppReleaseInfo { Version = "2.0.0", MinSupportedVersion = "1.5.0" }
        });

        var res = await svc.CheckAsync();

        Assert.True(res.AppUpdateAvailable);
        Assert.True(res.RequiresFullInstall, "1.0.0 is older than minSupportedVersion 1.5.0");
    }

    [Fact]
    public async Task Check_NoUpdate_WhenSameVersion()
    {
        var svc = MakeService("1.0.0", new UpdateFeed { Product = "P", Latest = new AppReleaseInfo { Version = "1.0.0" } });

        var res = await svc.CheckAsync();

        Assert.True(res.Succeeded);
        Assert.False(res.AppUpdateAvailable);
        Assert.False(res.AnyUpdateAvailable);
    }

    [Fact]
    public async Task Check_ReturnsNamedError_OnMalformedFeed()
    {
        var svc = new AppUpdateService(
            new UpdateSettings { FeedUrl = "feed.json", CurrentVersion = "1.0.0" },
            new UpdateFeedClient(new FakeTransport(text: "not json")));

        var res = await svc.CheckAsync();

        Assert.False(res.Succeeded);
        Assert.NotNull(res.Error);
        Assert.False(res.AppUpdateAvailable);
    }

    [Fact]
    public async Task Check_Disabled_ShortCircuits()
    {
        var svc = new AppUpdateService(new UpdateSettings { FeedUrl = "feed.json", Disabled = true, CurrentVersion = "1.0.0" });

        var res = await svc.CheckAsync();

        Assert.False(res.Succeeded);
        Assert.Contains("disabled", res.Error!, StringComparison.OrdinalIgnoreCase);
    }

    // ── DI ──

    [Fact]
    public void Di_ResolvesAppUpdateService_FromSettings()
    {
        var services = new ServiceCollection();
        services.AddBeepAppUpdates(new UpdateSettings { FeedUrl = "https://h/feed.json", Channel = "beta", Mode = UpdateMode.Required, CurrentVersion = "1.0.0" });
        using var sp = services.BuildServiceProvider();

        var svc = sp.GetService<IAppUpdateService>();

        Assert.NotNull(svc);
        Assert.Equal("https://h/feed.json", svc!.Settings.FeedUrl);
        Assert.Equal("beta", svc.Settings.Channel);
        Assert.Equal(UpdateMode.Required, svc.Settings.Mode);
    }
}
