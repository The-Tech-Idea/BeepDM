using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using TheTechIdea.Beep.Logger;
using TheTechIdea.Beep.Tools;

namespace TheTechIdea.Beep.Tools.PluginSystem
{
    /// <summary>
    /// Plugin messaging system for inter-plugin communication
    /// </summary>
    public class PluginMessageBus : IPluginMessageBus, IDisposable
    {
        private readonly ConcurrentDictionary<string, List<IMessageHandler>> _channelSubscribers = new();
        private readonly ConcurrentDictionary<string, Channel<object>> _channels = new();
        private readonly ConcurrentDictionary<string, Dictionary<string, IMessageHandler>> _pluginChannels = new();
        private readonly IDMLogger _logger;
        private bool _disposed = false;

        public PluginMessageBus(IDMLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Subscribe to a channel with a message handler
        /// </summary>
        public void Subscribe<T>(string channel, Action<T> handler)
        {
            if (string.IsNullOrWhiteSpace(channel) || handler == null)
                return;

            try
            {
                var messageHandler = new MessageHandler<T>(handler);
                
                if (!_channelSubscribers.ContainsKey(channel))
                {
                    _channelSubscribers[channel] = new List<IMessageHandler>();
                }

                _channelSubscribers[channel].Add(messageHandler);

                // Create channel if it doesn't exist
                if (!_channels.ContainsKey(channel))
                {
                    var channelOptions = new BoundedChannelOptions(1000)
                    {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = false,
                        SingleWriter = false
                    };
                    _channels[channel] = Channel.CreateBounded<object>(channelOptions);
                    
                    // Start message processing for this channel
                    _ = Task.Run(() => ProcessChannelMessages(channel));
                }

                _logger?.LogWithContext($"Subscribed to channel: {channel}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to subscribe to channel: {channel}", ex);
            }
        }

        /// <summary>
        /// Unsubscribe from a channel
        /// </summary>
        public void Unsubscribe(string channel)
        {
            if (string.IsNullOrWhiteSpace(channel))
                return;

            try
            {
                if (_channelSubscribers.TryRemove(channel, out var subscribers))
                {
                    foreach (var subscriber in subscribers)
                    {
                        subscriber.Dispose();
                    }
                }

                if (_channels.TryRemove(channel, out var channelWriter))
                {
                    channelWriter.Writer.Complete();
                }

                _logger?.LogWithContext($"Unsubscribed from channel: {channel}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to unsubscribe from channel: {channel}", ex);
            }
        }

        /// <summary>
        /// Publish a message to a channel
        /// </summary>
        public void Publish<T>(string channel, T message)
        {
            if (string.IsNullOrWhiteSpace(channel) || message == null)
                return;

            try
            {
                if (_channels.TryGetValue(channel, out var channelWriter))
                {
                    var success = channelWriter.Writer.TryWrite(message);
                    if (!success)
                    {
                        _logger?.LogWithContext($"Failed to write message to channel (channel full): {channel}", null);
                    }
                }
                else
                {
                    _logger?.LogWithContext($"Channel not found: {channel}", null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to publish message to channel: {channel}", ex);
            }
        }

        /// <summary>
        /// Send a direct message to a specific plugin
        /// </summary>
        public void SendToPlugin<T>(string pluginId, string channel, T message)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(channel) || message == null)
                return;

            try
            {
                var pluginChannel = $"{pluginId}:{channel}";
                
                if (_pluginChannels.TryGetValue(pluginId, out var pluginHandlers) &&
                    pluginHandlers.TryGetValue(channel, out var handler))
                {
                    handler.Handle(message);
                }
                else
                {
                    // Fallback to regular channel
                    Publish(pluginChannel, message);
                }

                _logger?.LogWithContext($"Message sent to plugin: {pluginId} on channel: {channel}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to send message to plugin: {pluginId}", ex);
            }
        }

        /// <summary>
        /// Register a plugin for direct messaging
        /// </summary>
        public void RegisterPluginForMessaging<T>(string pluginId, string channel, Action<T> handler)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(channel) || handler == null)
                return;

            try
            {
                if (!_pluginChannels.ContainsKey(pluginId))
                {
                    _pluginChannels[pluginId] = new Dictionary<string, IMessageHandler>();
                }

                _pluginChannels[pluginId][channel] = new MessageHandler<T>(handler);

                _logger?.LogWithContext($"Plugin registered for messaging: {pluginId} on channel: {channel}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to register plugin for messaging: {pluginId}", ex);
            }
        }

        /// <summary>
        /// Unregister a plugin from direct messaging
        /// </summary>
        public void UnregisterPlugin(string pluginId)
        {
            if (string.IsNullOrWhiteSpace(pluginId))
                return;

            try
            {
                if (_pluginChannels.TryRemove(pluginId, out var handlers))
                {
                    foreach (var handler in handlers.Values)
                    {
                        handler.Dispose();
                    }
                }

                _logger?.LogWithContext($"Plugin unregistered from messaging: {pluginId}", null);
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Failed to unregister plugin from messaging: {pluginId}", ex);
            }
        }

        /// <summary>
        /// Registers a plugin with the message bus
        /// </summary>
        public void RegisterPlugin(string pluginId, UnifiedPluginType pluginType)
        {
            // Implementation for plugin registration
            _logger?.LogWithContext($"Plugin registered with message bus: {pluginId} ({pluginType})", null);
        }

        /// <summary>
        /// Gets all registered plugins
        /// </summary>
        public List<string> GetRegisteredPlugins()
        {
            // Return list of registered plugin IDs
            return new List<string>();
        }

        /// <summary>
        /// Gets plugins by type
        /// </summary>
        public List<string> GetPluginsByType(UnifiedPluginType pluginType)
        {
            // Return list of plugin IDs for specific type
            return new List<string>();
        }

        /// <summary>
        /// Create a request-response channel
        /// </summary>
        public async Task<TResponse> RequestAsync<TRequest, TResponse>(string channel, TRequest request, TimeSpan timeout = default)
        {
            if (timeout == default)
                timeout = TimeSpan.FromSeconds(30);

            var responseChannel = $"{channel}:response:{Guid.NewGuid():N}";
            var responseReceived = false;
            TResponse response = default;

            // Subscribe to response channel
            Subscribe<TResponse>(responseChannel, r =>
            {
                response = r;
                responseReceived = true;
            });

            try
            {
                // Create request with response channel
                var requestMessage = new PluginRequest<TRequest>
                {
                    Data = request,
                    ResponseChannel = responseChannel,
                    RequestId = Guid.NewGuid().ToString()
                };

                // Publish request
                Publish(channel, requestMessage);

                // Wait for response with timeout
                var startTime = DateTime.UtcNow;
                while (!responseReceived && DateTime.UtcNow - startTime < timeout)
                {
                    await Task.Delay(50);
                }

                return response;
            }
            finally
            {
                // Clean up response channel
                Unsubscribe(responseChannel);
            }
        }

        /// <summary>
        /// Get channel statistics
        /// </summary>
        public Dictionary<string, ChannelStats> GetChannelStatistics()
        {
            var stats = new Dictionary<string, ChannelStats>();

            foreach (var kvp in _channelSubscribers)
            {
                stats[kvp.Key] = new ChannelStats
                {
                    ChannelName = kvp.Key,
                    SubscriberCount = kvp.Value.Count,
                    IsActive = _channels.ContainsKey(kvp.Key)
                };
            }

            return stats;
        }

        // Private helper methods
        private async Task ProcessChannelMessages(string channel)
        {
            if (!_channels.TryGetValue(channel, out var channelReader))
                return;

            try
            {
                await foreach (var message in channelReader.Reader.ReadAllAsync())
                {
                    if (_channelSubscribers.TryGetValue(channel, out var subscribers))
                    {
                        foreach (var subscriber in subscribers)
                        {
                            try
                            {
                                subscriber.Handle(message);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWithContext($"Error in message handler for channel: {channel}", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWithContext($"Error processing messages for channel: {channel}", ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Close all channels
                foreach (var channel in _channels.Values)
                {
                    channel.Writer.Complete();
                }

                // Dispose all handlers
                foreach (var subscribers in _channelSubscribers.Values)
                {
                    foreach (var subscriber in subscribers)
                    {
                        subscriber.Dispose();
                    }
                }

                foreach (var pluginHandlers in _pluginChannels.Values)
                {
                    foreach (var handler in pluginHandlers.Values)
                    {
                        handler.Dispose();
                    }
                }

                _channelSubscribers.Clear();
                _channels.Clear();
                _pluginChannels.Clear();

                _disposed = true;
            }
        }
    }

    // Helper interfaces and classes
    public interface IMessageHandler : IDisposable
    {
        void Handle(object message);
    }

    public class MessageHandler<T> : IMessageHandler
    {
        private readonly Action<T> _handler;

        public MessageHandler(Action<T> handler)
        {
            _handler = handler;
        }

        public void Handle(object message)
        {
            if (message is T typedMessage)
            {
                _handler(typedMessage);
            }
        }

        public void Dispose()
        {
            // Nothing to dispose for action delegates
        }
    }

    public class PluginRequest<T>
    {
        public T Data { get; set; }
        public string ResponseChannel { get; set; }
        public string RequestId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ChannelStats
    {
        public string ChannelName { get; set; }
        public int SubscriberCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastActivity { get; set; }
    }
}