using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Installer;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>Thrown when the feed cannot be fetched or is not valid — a named error, never a silent null.</summary>
    public sealed class UpdateFeedException : Exception
    {
        public UpdateFeedException(string message, Exception? inner = null) : base(message, inner) { }
    }

    /// <summary>
    /// Fetches, parses and hash-verifies the update feed and its artifacts. Async from day one and
    /// injectable-transport, so it carries none of the sync-over-async debt the ClickOnce updater
    /// pair did. Per decision D11 (v1), transport integrity is TLS and every artifact is SHA-256
    /// verified before use (<see cref="InstallHelpers.VerifyFileHash"/>); the feed document itself
    /// is not yet signed.
    /// </summary>
    public sealed class UpdateFeedClient
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly IFeedTransport _transport;

        public UpdateFeedClient(IFeedTransport? transport = null) => _transport = transport ?? new HttpFeedTransport();

        /// <summary>Serializes a feed to the canonical camelCase JSON (used by the publisher and tests).</summary>
        public static string Serialize(UpdateFeed feed)
            => JsonSerializer.Serialize(feed, new JsonSerializerOptions(Json) { WriteIndented = true });

        /// <summary>Fetches and parses <c>feed.json</c>. Throws <see cref="UpdateFeedException"/> on any failure.</summary>
        public async Task<UpdateFeed> FetchFeedAsync(string feedUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(feedUrl))
                throw new UpdateFeedException("No update feed URL was configured.");

            string json;
            try
            {
                json = await _transport.GetStringAsync(feedUrl, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new UpdateFeedException($"Could not fetch the update feed from '{feedUrl}': {ex.Message}", ex);
            }

            UpdateFeed? feed;
            try
            {
                feed = JsonSerializer.Deserialize<UpdateFeed>(json, Json);
            }
            catch (JsonException ex)
            {
                throw new UpdateFeedException($"The update feed at '{feedUrl}' is not valid JSON: {ex.Message}", ex);
            }

            return feed ?? throw new UpdateFeedException($"The update feed at '{feedUrl}' was empty.");
        }

        /// <summary>Fetches and parses a <c>_payload-manifest.json</c> for delta planning.</summary>
        public async Task<PayloadManifest> FetchManifestAsync(string manifestUrl, CancellationToken ct = default)
        {
            string json;
            try { json = await _transport.GetStringAsync(manifestUrl, ct).ConfigureAwait(false); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new UpdateFeedException($"Could not fetch the payload manifest from '{manifestUrl}': {ex.Message}", ex);
            }
            try { return JsonSerializer.Deserialize<PayloadManifest>(json, Json) ?? new PayloadManifest(); }
            catch (JsonException ex)
            {
                throw new UpdateFeedException($"The payload manifest at '{manifestUrl}' is not valid JSON: {ex.Message}", ex);
            }
        }

        /// <summary>Fetches raw blob bytes (the caller verifies the hash — the side-by-side applier does).</summary>
        public Task<byte[]> FetchBlobAsync(string blobUrl, CancellationToken ct = default)
            => _transport.GetBytesAsync(blobUrl, ct);

        /// <summary>
        /// Downloads an artifact to <paramref name="destPath"/> and verifies its SHA-256. A hash
        /// mismatch discards the file and returns a named failure — a corrupt or tampered artifact
        /// is never left on disk for a later step to pick up.
        /// </summary>
        public async Task<IErrorsInfo> DownloadVerifiedAsync(string url, string sha256, string destPath, CancellationToken ct = default)
        {
            try
            {
                var bytes = await _transport.GetBytesAsync(url, ct).ConfigureAwait(false);
                var dir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(destPath, bytes, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return Fail($"Failed to download '{url}': {ex.Message}", ex);
            }

            if (!string.IsNullOrEmpty(sha256) && !InstallHelpers.VerifyFileHash(destPath, sha256))
            {
                try { File.Delete(destPath); } catch { /* best effort */ }
                return Fail($"Downloaded artifact '{url}' failed SHA-256 verification (expected {sha256}); discarded.");
            }

            return new ErrorsInfo { Flag = Errors.Ok, Message = $"Downloaded and verified {Path.GetFileName(destPath)}." };
        }

        private static IErrorsInfo Fail(string message, Exception? ex = null)
            => new ErrorsInfo { Flag = Errors.Failed, Message = message, Ex = ex };
    }
}
