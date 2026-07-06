using System;

namespace TheTechIdea.Beep.SetUp
{
    /// <summary>
    /// Published by the driver-provision UI step when a NuGet package install completes.
    /// Carries the package id, version, success flag, and a human-readable message.
    /// </summary>
    public sealed class DriverPackageInstalledEventArgs : EventArgs
    {
        public DriverPackageInstalledEventArgs(string packageId, string version, bool success, string message)
        {
            PackageId = packageId;
            Version = version;
            Success = success;
            Message = message;
        }

        public string PackageId { get; }
        public string Version { get; }
        public bool Success { get; }
        public string Message { get; }
    }
}
