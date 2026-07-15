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
        /// <summary>
        /// Reports the outcome of testing <paramref name="connectionProperties"/>. A handler
        /// watching more than one connection needs the properties to know which one this
        /// result belongs to.
        /// </summary>
        public ConnectionTestCompletedEventArgs(
            ConnectionProperties? connectionProperties,
            bool success,
            string message)
        {
            ConnectionProperties = connectionProperties;
            Success = success;
            Message = message;
        }

        /// <summary>
        /// Reports an outcome without identifying the connection. Prefer the overload that
        /// takes <see cref="ConnectionProperties"/> so handlers can attribute the result.
        /// </summary>
        public ConnectionTestCompletedEventArgs(bool success, string message)
            : this(null, success, message)
        {
        }

        /// <summary>The connection that was tested, or null when the caller did not supply it.</summary>
        public ConnectionProperties? ConnectionProperties { get; }

        public bool Success { get; }
        public string Message { get; }
    }
}
