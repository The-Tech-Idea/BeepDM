using System;
using TheTechIdea.Beep.ConfigUtil;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Published by the connection UI step when the user saves a <see cref="ConnectionProperties"/>.
    /// </summary>
    public sealed class ConnectionSavedEventArgs : EventArgs
    {
        public ConnectionSavedEventArgs(ConnectionProperties connectionProperties)
        {
            ConnectionProperties = connectionProperties;
        }

        public ConnectionProperties ConnectionProperties { get; }
    }

    /// <summary>
    /// Published by the connection UI step when a connection test completes.
    /// </summary>
    public sealed class ConnectionTestCompletedEventArgs : EventArgs
    {
        public ConnectionTestCompletedEventArgs(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }
        public string Message { get; }
    }
}
