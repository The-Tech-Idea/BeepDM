using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TheTechIdea.Beep.Editor;

namespace BeepShell.Infrastructure
{
    /// <summary>
    /// Shell event types
    /// </summary>
    public enum ShellEventType
    {
        ShellStarted,
        ShellStopping,
        CommandExecuting,
        CommandExecuted,
        CommandFailed,
        ConnectionOpened,
        ConnectionClosed,
        DataSourceAdded,
        DataSourceRemoved,
        ConfigurationChanged,
        ProfileSwitched,
        ExtensionLoaded,
        ExtensionUnloaded,
        PluginReloaded
    }

    /// <summary>
    /// Shell event arguments
    /// </summary>
    public class ShellEventArgs : EventArgs
    {
        public ShellEventType EventType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Dictionary<string, object> Data { get; set; } = new();
        
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public void SetData(string key, object value)
        {
            Data[key] = value;
        }
    }

    /// <summary>
    /// Event bus for shell and extension communication
    /// </summary>
    public interface IShellEventBus
    {
        /// <summary>
        /// Subscribe to shell events
        /// </summary>
        void Subscribe(ShellEventType eventType, Action<ShellEventArgs> handler);

        /// <summary>
        /// Subscribe to shell events with async handler
        /// </summary>
        void Subscribe(ShellEventType eventType, Func<ShellEventArgs, Task> handler);

        /// <summary>
        /// Unsubscribe from shell events
        /// </summary>
        void Unsubscribe(ShellEventType eventType, Action<ShellEventArgs> handler);

        /// <summary>
        /// Publish an event
        /// </summary>
        Task PublishAsync(ShellEventType eventType, ShellEventArgs args = null);

        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Default implementation of shell event bus
    /// </summary>
    public class ShellEventBus : IShellEventBus
    {
        private readonly Dictionary<ShellEventType, List<Delegate>> _subscribers = new();
        private readonly object _lock = new();

        public void Subscribe(ShellEventType eventType, Action<ShellEventArgs> handler)
        {
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(eventType))
                {
                    _subscribers[eventType] = new List<Delegate>();
                }
                _subscribers[eventType].Add(handler);
            }
        }

        public void Subscribe(ShellEventType eventType, Func<ShellEventArgs, Task> handler)
        {
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(eventType))
                {
                    _subscribers[eventType] = new List<Delegate>();
                }
                _subscribers[eventType].Add(handler);
            }
        }

        public void Unsubscribe(ShellEventType eventType, Action<ShellEventArgs> handler)
        {
            lock (_lock)
            {
                if (_subscribers.ContainsKey(eventType))
                {
                    _subscribers[eventType].Remove(handler);
                }
            }
        }

        public async Task PublishAsync(ShellEventType eventType, ShellEventArgs args = null)
        {
            args ??= new ShellEventArgs { EventType = eventType };
            args.EventType = eventType;

            List<Delegate> handlers;
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(eventType))
                {
                    return;
                }
                handlers = new List<Delegate>(_subscribers[eventType]);
            }

            var tasks = new List<Task>();

            foreach (var handler in handlers)
            {
                try
                {
                    if (handler is Action<ShellEventArgs> syncHandler)
                    {
                        syncHandler(args);
                    }
                    else if (handler is Func<ShellEventArgs, Task> asyncHandler)
                    {
                        tasks.Add(asyncHandler(args));
                    }
                }
                catch (Exception)
                {
                    // Log error but don't break other handlers
                }
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }

        public int GetSubscriberCount(ShellEventType eventType)
        {
            lock (_lock)
            {
                return _subscribers.ContainsKey(eventType) 
                    ? _subscribers[eventType].Count 
                    : 0;
            }
        }
    }

    /// <summary>
    /// Extension helper for event subscriptions
    /// </summary>
    public static class ShellEventExtensions
    {
        /// <summary>
        /// Subscribe to command execution events
        /// </summary>
        public static void OnCommandExecuted(this IShellEventBus eventBus, Action<string, TimeSpan> handler)
        {
            eventBus.Subscribe(ShellEventType.CommandExecuted, args =>
            {
                var command = args.GetData<string>("command");
                var duration = args.GetData<TimeSpan>("duration");
                handler(command, duration);
            });
        }

        /// <summary>
        /// Subscribe to connection events
        /// </summary>
        public static void OnConnectionOpened(this IShellEventBus eventBus, Action<string> handler)
        {
            eventBus.Subscribe(ShellEventType.ConnectionOpened, args =>
            {
                var connectionName = args.GetData<string>("connectionName");
                handler(connectionName);
            });
        }

        /// <summary>
        /// Subscribe to configuration change events
        /// </summary>
        public static void OnConfigurationChanged(this IShellEventBus eventBus, Action handler)
        {
            eventBus.Subscribe(ShellEventType.ConfigurationChanged, args => handler());
        }
    }
}
