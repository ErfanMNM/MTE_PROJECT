using System.Data.SQLite;

namespace DMS_Project.Auth;

public class AuthRepository
{
    private readonly string _dbPath;

    public AuthRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    private SQLiteConnection Open()
    {
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var conn = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        conn.Open();
        new SQLiteCommand("PRAGMA journal_mode=WAL;", conn).ExecuteNonQuery();
        return conn;
    }

    public void EnsureSchema()
    {
        using var conn = Open();
        const string sql = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Email TEXT,
                Role TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                CreatedBy TEXT,
                LastLoginAt TEXT
            );
            CREATE INDEX IF NOT EXISTS IDX_Users_Role ON Users(Role);
            CREATE INDEX IF NOT EXISTS IDX_Users_IsActive ON Users(IsActive);
        ";
        using var cmd = new SQLiteCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    public int CountUsers()
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand("SELECT COUNT(*) FROM Users", conn);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void InsertUser(User u)
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand(@"
            INSERT INTO Users (Username, PasswordHash, DisplayName, Email, Role, IsActive, CreatedAt, CreatedBy, LastLoginAt)
            VALUES (@u, @h, @n, @e, @r, @a, @c, @cb, @ll)", conn);
        cmd.Parameters.AddWithValue("@u", u.Username);
        cmd.Parameters.AddWithValue("@h", u.PasswordHash);
        cmd.Parameters.AddWithValue("@n", u.DisplayName);
        cmd.Parameters.AddWithValue("@e", (object?)u.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@r", u.Role);
        cmd.Parameters.AddWithValue("@a", u.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@c", u.CreatedAt);
        cmd.Parameters.AddWithValue("@cb", (object?)u.CreatedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ll", (object?)u.LastLoginAt ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public User? FindByUsername(string username)
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand(
            "SELECT Id, Username, PasswordHash, DisplayName, Email, Role, IsActive, CreatedAt, CreatedBy, LastLoginAt FROM Users WHERE Username = @u",
            conn);
        cmd.Parameters.AddWithValue("@u", username);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return ReadUser(reader);
    }

    public User? FindById(int id)
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand(
            "SELECT Id, Username, PasswordHash, DisplayName, Email, Role, IsActive, CreatedAt, CreatedBy, LastLoginAt FROM Users WHERE Id = @id",
            conn);
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return ReadUser(reader);
    }

    public List<User> ListAll()
    {
        var list = new List<User>();
        using var conn = Open();
        using var cmd = new SQLiteCommand(
            "SELECT Id, Username, PasswordHash, DisplayName, Email, Role, IsActive, CreatedAt, CreatedBy, LastLoginAt FROM Users ORDER BY Id",
            conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(ReadUser(reader));
        }
        return list;
    }

    public void UpdateLastLogin(int id, string timestampUtc)
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand("UPDATE Users SET LastLoginAt = @t WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@t", timestampUtc);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdateUser(int id, string? displayName, string? email, string? role, bool? isActive)
    {
        var sets = new List<string>();
        var parameters = new List<SQLiteParameter>();
        if (displayName != null)
        {
            sets.Add("DisplayName = @dn");
            parameters.Add(new SQLiteParameter("@dn", displayName));
        }
        if (email != null)
        {
            sets.Add("Email = @em");
            parameters.Add(new SQLiteParameter("@em", email));
        }
        if (role != null)
        {
            sets.Add("Role = @r");
            parameters.Add(new SQLiteParameter("@r", role));
        }
        if (isActive.HasValue)
        {
            sets.Add("IsActive = @a");
            parameters.Add(new SQLiteParameter("@a", isActive.Value ? 1 : 0));
        }
        if (sets.Count == 0) return;

        var sql = $"UPDATE Users SET {string.Join(", ", sets)} WHERE Id = @id";
        using var conn = Open();
        using var cmd = new SQLiteCommand(sql, conn);
        foreach (var p in parameters) cmd.Parameters.Add(new SQLiteParameter(p.ParameterName, p.Value));
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public void UpdatePassword(int id, string newHash)
    {
        using var conn = Open();
        using var cmd = new SQLiteCommand("UPDATE Users SET PasswordHash = @h WHERE Id = @id", conn);
        cmd.Parameters.AddWithValue("@h", newHash);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    private static User ReadUser(SQLiteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Username = reader.GetString(1),
        PasswordHash = reader.GetString(2),
        DisplayName = reader.GetString(3),
        Email = reader.IsDBNull(4) ? null : reader.GetString(4),
        Role = reader.GetString(5),
        IsActive = reader.GetInt32(6) == 1,
        CreatedAt = reader.GetString(7),
        CreatedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
        LastLoginAt = reader.IsDBNull(9) ? null : reader.GetString(9)
    };
}