using System.Data.SQLite;
using System.Text;

namespace DMS_Project.Audit;

public class AuditRepository
{
    private readonly string _dbPath;

    public AuditRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    private SQLiteConnection Open()
    {
        var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        conn.Open();
        new SQLiteCommand("PRAGMA journal_mode=WAL;", conn).ExecuteNonQuery();
        return conn;
    }

    public void Insert(AuditEvent e)
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand(@"
            INSERT INTO AuditEvents (
                EventId, TimestampUtc, ActorId, ActorUsername, ActorRole, ActorSource,
                CorrelationId, Source, HttpMethod, HttpPath, HttpStatusCode, ClientIp, UserAgent,
                Action, EntityType, EntityId, ParentEntityType, ParentEntityId,
                Outcome, ErrorMessage, BeforeJson, AfterJson, ChangedFields, MetadataJson,
                ApiGroup, DurationMs)
            VALUES (
                @eid, @ts, @aid, @auser, @arole, @asrc,
                @cid, @src, @hm, @hp, @hsc, @cip, @ua,
                @act, @et, @eid2, @pet, @peid,
                @out, @err, @bj, @aj, @cf, @meta,
                @grp, @dur)", conn);

        cmd.Parameters.AddWithValue("@eid", e.EventId);
        cmd.Parameters.AddWithValue("@ts", e.TimestampUtc);
        cmd.Parameters.AddWithValue("@aid", (object?)e.ActorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@auser", e.ActorUsername);
        cmd.Parameters.AddWithValue("@arole", e.ActorRole);
        cmd.Parameters.AddWithValue("@asrc", e.ActorSource);
        cmd.Parameters.AddWithValue("@cid", e.CorrelationId);
        cmd.Parameters.AddWithValue("@src", e.Source);
        cmd.Parameters.AddWithValue("@hm", (object?)e.HttpMethod ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@hp", (object?)e.HttpPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@hsc", (object?)e.HttpStatusCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cip", (object?)e.ClientIp ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ua", (object?)e.UserAgent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@act", e.Action);
        cmd.Parameters.AddWithValue("@et", e.EntityType);
        cmd.Parameters.AddWithValue("@eid2", (object?)e.EntityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@pet", (object?)e.ParentEntityType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@peid", (object?)e.ParentEntityId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@out", e.Outcome);
        cmd.Parameters.AddWithValue("@err", (object?)e.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bj", (object?)e.BeforeJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@aj", (object?)e.AfterJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@cf", (object?)e.ChangedFields ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@meta", (object?)e.MetadataJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@grp", (object?)e.ApiGroup ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dur", (object?)e.DurationMs ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public PagedAuditDto Query(AuditQuery q)
    {
        var (where, parameters) = BuildWhere(q);

        using var conn = Open();

        var countSql = $"SELECT COUNT(*) FROM AuditEvents {where}";
        using var countCmd = new SQLiteCommand(countSql, conn);
        foreach (var p in parameters) countCmd.Parameters.Add(new SQLiteParameter(p.ParameterName, p.Value));
        int total = Convert.ToInt32(countCmd.ExecuteScalar());

        int pageIndex = q.PageIndex < 1 ? 1 : q.PageIndex;
        int pageSize = q.PageSize < 1 ? 50 : Math.Min(q.PageSize, 500);
        int offset = (pageIndex - 1) * pageSize;

        var sql = $@"
            SELECT Id, EventId, TimestampUtc, ActorId, ActorUsername, ActorRole, ActorSource,
                   CorrelationId, Source, HttpMethod, HttpPath, HttpStatusCode, ClientIp, UserAgent,
                   Action, EntityType, EntityId, ParentEntityType, ParentEntityId,
                   Outcome, ErrorMessage, BeforeJson, AfterJson, ChangedFields, MetadataJson,
                   ApiGroup, DurationMs
            FROM AuditEvents {where}
            ORDER BY TimestampUtc DESC, Id DESC
            LIMIT @lim OFFSET @off";

        using var cmd = new SQLiteCommand(sql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(new SQLiteParameter(p.ParameterName, p.Value));
        cmd.Parameters.AddWithValue("@lim", pageSize);
        cmd.Parameters.AddWithValue("@off", offset);

        var items = new List<AuditEvent>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(ReadEvent(reader));
        }
        return new PagedAuditDto { Items = items, TotalCount = total, PageIndex = pageIndex, PageSize = pageSize };
    }

    public AuditEvent? FindByEventId(string eventId)
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand(
            "SELECT Id, EventId, TimestampUtc, ActorId, ActorUsername, ActorRole, ActorSource, CorrelationId, Source, HttpMethod, HttpPath, HttpStatusCode, ClientIp, UserAgent, Action, EntityType, EntityId, ParentEntityType, ParentEntityId, Outcome, ErrorMessage, BeforeJson, AfterJson, ChangedFields, MetadataJson, ApiGroup, DurationMs FROM AuditEvents WHERE EventId = @eid",
            conn);
        cmd.Parameters.AddWithValue("@eid", eventId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return ReadEvent(reader);
    }

    public IEnumerable<AuditEvent> Stream(AuditQuery q, int maxRows)
    {
        var (where, parameters) = BuildWhere(q);
        using var conn = Open();
        var sql = $@"
            SELECT Id, EventId, TimestampUtc, ActorId, ActorUsername, ActorRole, ActorSource,
                   CorrelationId, Source, HttpMethod, HttpPath, HttpStatusCode, ClientIp, UserAgent,
                   Action, EntityType, EntityId, ParentEntityType, ParentEntityId,
                   Outcome, ErrorMessage, BeforeJson, AfterJson, ChangedFields, MetadataJson,
                   ApiGroup, DurationMs
            FROM AuditEvents {where}
            ORDER BY TimestampUtc DESC, Id DESC
            LIMIT @lim";
        using var cmd = new SQLiteCommand(sql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(new SQLiteParameter(p.ParameterName, p.Value));
        cmd.Parameters.AddWithValue("@lim", maxRows);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return ReadEvent(reader);
        }
    }

    private static (string where, List<SQLiteParameter> parameters) BuildWhere(AuditQuery q)
    {
        var clauses = new List<string>();
        var parameters = new List<SQLiteParameter>();

        if (q.FromUtc.HasValue)
        {
            clauses.Add("TimestampUtc >= @from");
            parameters.Add(new SQLiteParameter("@from", q.FromUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
        }
        if (q.ToUtc.HasValue)
        {
            clauses.Add("TimestampUtc <= @to");
            parameters.Add(new SQLiteParameter("@to", q.ToUtc.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")));
        }
        if (!string.IsNullOrWhiteSpace(q.EntityType))
        {
            clauses.Add("EntityType = @et");
            parameters.Add(new SQLiteParameter("@et", q.EntityType));
        }
        if (!string.IsNullOrWhiteSpace(q.EntityId))
        {
            clauses.Add("EntityId = @eid");
            parameters.Add(new SQLiteParameter("@eid", q.EntityId));
        }
        if (!string.IsNullOrWhiteSpace(q.ActorUsername))
        {
            clauses.Add("ActorUsername = @actor");
            parameters.Add(new SQLiteParameter("@actor", q.ActorUsername));
        }
        if (!string.IsNullOrWhiteSpace(q.Action))
        {
            clauses.Add("Action = @act");
            parameters.Add(new SQLiteParameter("@act", q.Action));
        }
        if (!string.IsNullOrWhiteSpace(q.Outcome))
        {
            clauses.Add("Outcome = @out");
            parameters.Add(new SQLiteParameter("@out", q.Outcome));
        }
        if (!string.IsNullOrWhiteSpace(q.Source))
        {
            clauses.Add("Source = @src");
            parameters.Add(new SQLiteParameter("@src", q.Source));
        }
        if (!string.IsNullOrWhiteSpace(q.CorrelationId))
        {
            clauses.Add("CorrelationId = @cid");
            parameters.Add(new SQLiteParameter("@cid", q.CorrelationId));
        }

        var where = clauses.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", clauses);
        return (where, parameters);
    }

    private static AuditEvent ReadEvent(SQLiteDataReader r) => new()
    {
        Id = r.GetInt64(0),
        EventId = r.GetString(1),
        TimestampUtc = r.GetString(2),
        ActorId = r.IsDBNull(3) ? null : r.GetInt32(3),
        ActorUsername = r.GetString(4),
        ActorRole = r.GetString(5),
        ActorSource = r.GetString(6),
        CorrelationId = r.GetString(7),
        Source = r.GetString(8),
        HttpMethod = r.IsDBNull(9) ? null : r.GetString(9),
        HttpPath = r.IsDBNull(10) ? null : r.GetString(10),
        HttpStatusCode = r.IsDBNull(11) ? null : r.GetInt32(11),
        ClientIp = r.IsDBNull(12) ? null : r.GetString(12),
        UserAgent = r.IsDBNull(13) ? null : r.GetString(13),
        Action = r.GetString(14),
        EntityType = r.GetString(15),
        EntityId = r.IsDBNull(16) ? null : r.GetString(16),
        ParentEntityType = r.IsDBNull(17) ? null : r.GetString(17),
        ParentEntityId = r.IsDBNull(18) ? null : r.GetString(18),
        Outcome = r.GetString(19),
        ErrorMessage = r.IsDBNull(20) ? null : r.GetString(20),
        BeforeJson = r.IsDBNull(21) ? null : r.GetString(21),
        AfterJson = r.IsDBNull(22) ? null : r.GetString(22),
        ChangedFields = r.IsDBNull(23) ? null : r.GetString(23),
        MetadataJson = r.IsDBNull(24) ? null : r.GetString(24),
        ApiGroup = r.IsDBNull(25) ? null : r.GetString(25),
        DurationMs = r.IsDBNull(26) ? null : r.GetInt32(26)
    };
}