using System;

namespace TheTechIdea.Beep.ConfigUtil
{
    /// <summary>A committed catalog change whose observer failed; never contains raw exception or connection data.</summary>
    public sealed class ConnectionCatalogNotificationFailureEventArgs : EventArgs
    {
        public string Operation { get; }
        public ConnectionStorageScope Scope { get; }
        public string ExceptionType { get; }

        public ConnectionCatalogNotificationFailureEventArgs(string operation, ConnectionStorageScope scope, string exceptionType)
        {
            Operation = operation;
            Scope = scope;
            ExceptionType = exceptionType;
        }
    }
}
