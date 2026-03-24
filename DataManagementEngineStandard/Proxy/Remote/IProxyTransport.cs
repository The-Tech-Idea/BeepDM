using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Proxy.Remote
{
    // ─────────────────────────────────────────────────────────────────────────
    //  IProxyTransport — pluggable network transport abstraction
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Abstracts the network channel between a <see cref="RemoteProxyDataSource"/>
    /// (client) and a worker's <see cref="ProxyRemoteRequestDispatcher"/> (server).
    ///
    /// Implementations provided:
    /// <list type="bullet">
    ///   <item><see cref="HttpProxyTransport"/> — HTTP/JSON over System.Net.Http (zero extra NuGet)</item>
    /// </list>
    ///
    /// You can implement this interface to use gRPC, NATS, RabbitMQ, named pipes,
    /// or an in-process test double without changing any proxy logic.
    /// </summary>
    public interface IProxyTransport : IDisposable
    {
        /// <summary>
        /// Human-readable address of the remote worker node, e.g.
        /// <c>"http://worker-b:5100"</c> or <c>"grpc://worker-c:5200"</c>.
        /// Used in log messages and diagnostics only — never parsed by the proxy.
        /// </summary>
        string NodeAddress { get; }

        /// <summary>
        /// Sends a <see cref="ProxyRemoteRequest"/> to the remote worker and
        /// returns the worker's <see cref="ProxyRemoteResponse"/>.
        ///
        /// Transport exceptions (network unreachable, timeout) should be thrown
        /// rather than swallowed — let the caller (ProxyCluster's retry policy)
        /// decide how to handle them.
        /// </summary>
        Task<ProxyRemoteResponse> SendAsync(
            ProxyRemoteRequest request,
            CancellationToken   ct = default);

        /// <summary>
        /// Fires a lightweight liveness ping to the remote worker.
        /// Returns <c>true</c> when the worker is reachable and healthy.
        /// Must complete within a few hundred milliseconds so that the
        /// ProxyCluster probe loop runs on schedule.
        /// </summary>
        Task<bool> PingAsync(CancellationToken ct = default);
    }
}
