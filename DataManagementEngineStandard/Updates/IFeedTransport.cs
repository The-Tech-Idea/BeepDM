using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Updates
{
    /// <summary>
    /// Fetches feed text and artifact bytes. Abstracted so the feed client is transport-agnostic
    /// (HTTPS, a local/UNC folder feed, or a fake in tests) — the whole point of decision D10 is
    /// that the client only ever sees URLs, so a hosting change never touches update logic.
    /// </summary>
    public interface IFeedTransport
    {
        Task<string> GetStringAsync(string url, CancellationToken ct = default);
        Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default);
    }

    /// <summary>
    /// Default transport: HTTPS via <see cref="HttpClient"/>, with a transparent fallback to the
    /// local filesystem when the URL is a <c>file://</c> URI or a plain path — so a
    /// <c>LocalNugetFiles</c>-style LAN folder feed works with no extra configuration.
    /// </summary>
    public sealed class HttpFeedTransport : IFeedTransport
    {
        private readonly HttpClient _http;

        public HttpFeedTransport(HttpClient? http = null) => _http = http ?? new HttpClient();

        public Task<string> GetStringAsync(string url, CancellationToken ct = default)
            => IsLocal(url, out var path) ? File.ReadAllTextAsync(path, ct) : _http.GetStringAsync(url, ct);

        public Task<byte[]> GetBytesAsync(string url, CancellationToken ct = default)
            => IsLocal(url, out var path) ? File.ReadAllBytesAsync(path, ct) : _http.GetByteArrayAsync(url, ct);

        private static bool IsLocal(string url, out string path)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.IsFile) { path = uri.LocalPath; return true; }
                path = ""; return false; // http/https/etc.
            }
            path = url; return true; // not an absolute URI → treat as a filesystem path
        }
    }
}
