using System.Data.SQLite;

namespace DMS_Project.Audit;

public class AuditDbInitializer
{
    private readonly string _dbPath;
    private readonly ILogger<AuditDbInitializer> _logger;

    public AuditDbInitializer(string dbPath, ILogger<AuditDbInitializer> logger)
    {
        _dbPath = dbPath;
        _logger = logger;
    }

    public void EnsureCreated()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        conn.Open();
        new SQLiteCommand("PRAGMA journal_mode=WAL;", conn).ExecuteNonQuery();

        const string sql = @"
            CREATE TABLE IF NOT EXISTS AuditEvents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EventId TEXT NOT NULL UNIQUE,
                TimestampUtc TEXT NOT NULL,
                ActorId INTEGER,
                ActorUsername TEXT NOT NULL,
                ActorRole TEXT NOT NULL,
                ActorSource TEXT NOT NULL,
                CorrelationId TEXT NOT NULL,
                Source TEXT NOT NULL,
                HttpMethod TEXT,
                HttpPath TEXT,
                HttpStatusCode INTEGER,
                ClientIp TEXT,
                UserAgent TEXT,
                Action TEXT NOT NULL,
                EntityType TEXT NOT NULL,
                EntityId TEXT,
                ParentEntityType TEXT,
                ParentEntityId TEXT,
                Outcome TEXT NOT NULL,
                ErrorMessage TEXT,
                BeforeJson TEXT,
                AfterJson TEXT,
                ChangedFields TEXT,
                MetadataJson TEXT,
                ApiGroup TEXT,
                DurationMs INTEGER
            );
            CREATE INDEX IF NOT EXISTS IDX_Audit_Timestamp ON AuditEvents(TimestampUtc DESC);
            CREATE INDEX IF NOT EXISTS IDX_Audit_Entity ON AuditEvents(EntityType, EntityId, TimestampUtc DESC);
            CREATE INDEX IF NOT EXISTS IDX_Audit_Actor ON AuditEvents(ActorUsername, TimestampUtc DESC);
            CREATE INDEX IF NOT EXISTS IDX_Audit_Action ON AuditEvents(Action, TimestampUtc DESC);
            CREATE INDEX IF NOT EXISTS IDX_Audit_Correlation ON AuditEvents(CorrelationId);
            CREATE INDEX IF NOT EXISTS IDX_Audit_Outcome ON AuditEvents(Outcome, TimestampUtc DESC);
        ";
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.ExecuteNonQuery();

        _logger.LogInformation("Audit database initialized at {Path}", _dbPath);
    }
}