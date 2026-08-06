namespace DMS_Project.Config;

/// <summary>
/// Root configuration model for application settings.
/// </summary>
public class AppConfig
{
    // PLC Settings
    public string? PLC_IP { get; set; }
    public int PLC_Port { get; set; }

    //API Settings
    public string? API_HostIP { get; set; } = "0.0.0.0";
    public int API_Port { get; set; }

    // Camera Settings
    public string? Camera_Ip { get; set; }
    public int Camera_Port { get; set; }

    // Application Settings
    public bool AutoStart { get; set; }

    // PLC ACK V2 (camera pipeline) — chờ CurrentID/CurrentStatus confirm lane đã gửi
    public int CameraAckTimeoutMs { get; set; } = 500;
    public int CameraAckPollIntervalMs { get; set; } = 10;

    // ===== Auth / JWT =====
    public string JwtSecret { get; set; } = "CHANGE_ME_DMS_PROJECT_DEV_SECRET_KEY_32B_MIN";
    public string JwtIssuer { get; set; } = "DMS";
    public string JwtAudience { get; set; } = "DMS_Clients";
    public int JwtExpirationMinutes { get; set; } = 480;
    public string InitialAdminUsername { get; set; } = "admin";
    public string InitialAdminPassword { get; set; } = "admin@123";
    public string InitialAdminDisplayName { get; set; } = "System Administrator";
    public string AuthDbPath { get; set; } = @"C:\DMS\Auth\auth.db";

    // ===== Audit =====
    public string AuditDbPath { get; set; } = @"C:\DMS\Audit\audit.db";
    public int AuditRetentionDays { get; set; } = 0;

    /// <summary>
    /// Sets all properties to their default values.
    /// </summary>
    public void SetDefault()
    {
        PLC_IP = "192.168.1.1";
        PLC_Port = 9600;

        Camera_Ip = "127.0.0.1";
        Camera_Port = 2001;

        API_HostIP = "127.0.0.1";
        API_Port = 9999;

        AutoStart = true;

        CameraAckTimeoutMs = 500;
        CameraAckPollIntervalMs = 10;

        // Auth defaults
        JwtSecret = "CHANGE_ME_DMS_PROJECT_DEV_SECRET_KEY_32B_MIN";
        JwtIssuer = "DMS";
        JwtAudience = "DMS_Clients";
        JwtExpirationMinutes = 480;
        InitialAdminUsername = "admin";
        InitialAdminPassword = "admin@123";
        InitialAdminDisplayName = "System Administrator";
        AuthDbPath = @"C:\DMS\Auth\auth.db";

        // Audit defaults
        AuditDbPath = @"C:\DMS\Audit\audit.db";
        AuditRetentionDays = 0;
    }
}