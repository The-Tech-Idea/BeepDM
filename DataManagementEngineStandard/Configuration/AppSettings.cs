using System.ComponentModel.DataAnnotations;
using TheTechIdea.Beep.Utilities;

namespace TheTechIdea.Beep.Configuration
{
    public class AppSettings
    {
        public string Environment { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Beep Application";
        public string Version { get; set; } = "1.0.0";
        public DatabaseSettings Database { get; set; } = new();
        public AuthenticationSettings Authentication { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
        public FeatureFlags Features { get; set; } = new();
        public UiSettings UI { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
    }

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = "Data Source=app.db;Version=3;";
        public DataSourceType DataSourceType { get; set; } = DataSourceType.SqlLite;
        public DatasourceCategory Category { get; set; } = DatasourceCategory.RDBMS;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableLogging { get; set; }
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 1;
        public string DriverName { get; set; } = "SqliteDatasourceCore";
        public string DriverVersion { get; set; } = "1.0.0";
    }

    public class AuthenticationSettings
    {
        public bool RememberUserCredentials { get; set; }
        public int SessionTimeoutMinutes { get; set; } = 60;
        public int MaxLoginAttempts { get; set; } = 3;
        public int AccountLockoutMinutes { get; set; } = 15;
        public bool RequirePasswordComplexity { get; set; } = true;
        public int MinPasswordLength { get; set; } = 6;
        public bool EnableTwoFactorAuth { get; set; }
        public string DefaultUsername { get; set; } = "";
        public bool AutoLoginInDevelopment { get; set; }
    }

    public class LoggingSettings
    {
        public BeepLogLevel MinimumLevel { get; set; } = BeepLogLevel.Information;
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableConsoleLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "Logs";
        public int MaxLogFileSizeMB { get; set; } = 50;
        public int MaxLogFiles { get; set; } = 10;
        public bool EnablePerformanceLogging { get; set; }
    }

    public class FeatureFlags
    {
        public bool EnableAdvancedReporting { get; set; } = true;
        public bool EnableDataExport { get; set; } = true;
        public bool EnableDataImport { get; set; } = true;
        public bool EnableUserManagement { get; set; }
        public bool EnableAuditTrail { get; set; }
        public bool EnableDashboardWidgets { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public bool EnableAutoSave { get; set; } = true;
        public bool EnableOfflineMode { get; set; }
        public bool EnableDarkMode { get; set; } = true;
    }

    public class UiSettings
    {
        public string DefaultTheme { get; set; } = "DefaultTheme";
        public bool RememberWindowSize { get; set; } = true;
        public bool RememberWindowPosition { get; set; } = true;
        public bool EnableAnimations { get; set; } = true;
        public bool ShowToolTips { get; set; } = true;
        public int AutoSaveIntervalMinutes { get; set; } = 5;
        public bool EnableStatusBar { get; set; } = true;
        public bool EnableToolbar { get; set; } = true;
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public string TimeFormat { get; set; } = "HH:mm:ss";
        public string CurrencyFormat { get; set; } = "C2";
    }

    public class SecuritySettings
    {
        public bool EnableEncryption { get; set; } = true;
        public bool EnableDataMasking { get; set; }
        public bool RequireSecureConnection { get; set; }
        public int InactivityTimeoutMinutes { get; set; } = 30;
        public bool EnableAuditLogging { get; set; }
        public bool ValidateDataIntegrity { get; set; } = true;
        public int MaxFileUploadSizeMB { get; set; } = 100;
    }

    public enum BeepLogLevel
    {
        Trace, Debug, Information, Warning, Error, Critical, None
    }
}
