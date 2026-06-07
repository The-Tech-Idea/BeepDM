using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheTechIdea.Beep.ConfigUtil;
using TheTechIdea.Beep.Editor;

namespace TheTechIdea.Beep.Winform.Controls
{
    public enum ConnectionHealthStatus
    {
        Unknown,
        Healthy,
        Degraded,
        Unhealthy
    }

    public sealed class ConnectionHealthChangedEventArgs : EventArgs
    {
        public string ConnectionName { get; }
        public ConnectionHealthStatus PreviousStatus { get; }
        public ConnectionHealthStatus CurrentStatus { get; }
        public string? Message { get; }

        public ConnectionHealthChangedEventArgs(string name, ConnectionHealthStatus previous, ConnectionHealthStatus current, string? message = null)
        {
            ConnectionName = name;
            PreviousStatus = previous;
            CurrentStatus = current;
            Message = message;
        }
    }

    public sealed class ConnectionHealthMonitor : IDisposable
    {
        private readonly IDMEEditor _editor;
        private readonly Dictionary<string, ConnectionHealthStatus> _healthStates = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _healthLock = new();
        private System.Threading.Timer? _timer;
        private TimeSpan _interval;
        private bool _disposed;

        public event EventHandler<ConnectionHealthChangedEventArgs>? HealthChanged;

        public ConnectionHealthMonitor(IDMEEditor editor, TimeSpan? checkInterval = null)
        {
            _editor = editor ?? throw new ArgumentNullException(nameof(editor));
            _interval = checkInterval ?? TimeSpan.FromMinutes(5);
        }

        public void Start(TimeSpan? interval = null, bool immediateCheck = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ConnectionHealthMonitor));
            if (interval.HasValue) _interval = interval.Value;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(_ =>
            {
                _ = Task.Run(async () =>
                {
                    try { await CheckAllAsync(); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ConnectionHealthMonitor.Timer] {ex.Message}"); }
                });
            }, null, immediateCheck ? TimeSpan.Zero : _interval, _interval);
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public async Task<ConnectionHealthStatus> CheckAsync(string connectionName)
        {
            if (string.IsNullOrWhiteSpace(connectionName))
                return ConnectionHealthStatus.Unknown;

            ConnectionHealthStatus previous;
            lock (_healthLock)
            {
                if (!_healthStates.ContainsKey(connectionName))
                    _healthStates[connectionName] = ConnectionHealthStatus.Unknown;
                previous = _healthStates[connectionName];
            }

            var status = await TestConnectionAsync(connectionName);

            if (previous != status)
            {
                var message = status == ConnectionHealthStatus.Healthy
                    ? "Connection restored."
                    : "Connection lost or degraded.";
                lock (_healthLock) { _healthStates[connectionName] = status; }
                HealthChanged?.Invoke(this, new ConnectionHealthChangedEventArgs(connectionName, previous, status, message));
            }

            return status;
        }

        public async Task CheckAllAsync()
        {
            var connections = _editor.ConfigEditor?.DataConnections;
            if (connections == null) return;

            foreach (var conn in connections)
            {
                if (string.IsNullOrWhiteSpace(conn.ConnectionName)) continue;
                await CheckAsync(conn.ConnectionName);
            }
        }

        public ConnectionHealthStatus GetStatus(string connectionName)
        {
            lock (_healthLock)
            {
                return _healthStates.TryGetValue(connectionName, out var status) ? status : ConnectionHealthStatus.Unknown;
            }
        }

        public IReadOnlyDictionary<string, ConnectionHealthStatus> GetAllStatuses()
        {
            lock (_healthLock)
            {
                return new Dictionary<string, ConnectionHealthStatus>(_healthStates, StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task<bool> ReconnectAsync(string connectionName)
        {
            try
            {
                _editor.CloseDataSource(connectionName);
                var state = _editor.OpenDataSource(connectionName);
                return state == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }

        private async Task<ConnectionHealthStatus> TestConnectionAsync(string connectionName)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                return await Task.Run(() =>
                {
                    timeoutCts.Token.ThrowIfCancellationRequested();

                    var ds = _editor.GetDataSource(connectionName);
                    if (ds == null)
                    {
                        _editor.OpenDataSource(connectionName);
                        ds = _editor.GetDataSource(connectionName);
                    }

                    if (ds?.ConnectionStatus == System.Data.ConnectionState.Open)
                        return ConnectionHealthStatus.Healthy;

                    var state = _editor.OpenDataSource(connectionName);
                    return state == System.Data.ConnectionState.Open
                        ? ConnectionHealthStatus.Healthy
                        : ConnectionHealthStatus.Unhealthy;
                }, timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                return ConnectionHealthStatus.Degraded;
            }
            catch (Exception)
            {
                return ConnectionHealthStatus.Unhealthy;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
            _healthStates.Clear();
            HealthChanged = null;
        }
    }
}
