using System;

namespace TheTechIdea.Beep.NuGet
{
    /// <summary>
    /// Configuration for a NuGet package source
    /// </summary>
    public class NuGetSourceConfig
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsLocal { get; set; }
        public int Priority { get; set; } = 100;
        public string Username { get; set; }
        public string Password { get; set; }
        public string ApiKey { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public DateTime? LastChecked { get; set; }
        public bool IsHealthy { get; set; } = true;
    }
}
