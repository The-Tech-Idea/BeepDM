namespace TheTechIdea.Beep.Installer
{
    /// <summary>Branding configuration for the installer UI.</summary>
    public class InstallerBranding
    {
        public string ProductName { get; set; } = "Beep";
        public string WelcomeTitle { get; set; } = "Welcome to Beep Setup";
        public string WelcomeBannerPath { get; set; } = "";
        public string SidebarBackgroundColor { get; set; } = "#1E1E28";
        public string SidebarTextColor { get; set; } = "#FFFFFF";
        public string AccentColor { get; set; } = "#2962FF";
        public string WindowTitle { get; set; } = "Beep Installer";
        public string LicenseFile { get; set; } = "";
        public string PublisherName { get; set; } = "The Tech Idea";
        public string PublisherUrl { get; set; } = "https://github.com/The-Tech-Idea";
        public string SupportEmail { get; set; } = "";
        public string ProductIconPath { get; set; } = "";
        public bool ShowEula { get; set; } = true;
        public bool AllowComponentSelection { get; set; } = true;
        public bool AllowPathChange { get; set; } = true;
        public string DefaultTheme { get; set; } = "Modern";
    }
}
