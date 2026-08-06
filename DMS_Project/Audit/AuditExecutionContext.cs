namespace DMS_Project.Audit;

/// <summary>
/// AsyncLocal-scoped context. Populated by middleware for HTTP and cloned for queue workers.
/// </summary>
public class AuditExecutionContext
{
    private static readonly AsyncLocal<AuditExecutionContext?> _current = new();

    public static AuditExecutionContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public int? ActorId { get; set; }
    public string ActorUsername { get; set; } = "system";
    public string ActorRole { get; set; } = "Anonymous";
    public string ActorSource { get; set; } = AuditActorSources.System;
    public string Source { get; set; } = AuditSources.Http;
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public string? HttpMethod { get; set; }
    public string? HttpPath { get; set; }
    public string? ApiGroup { get; set; }
    public long StartTicks { get; set; } = DateTime.UtcNow.Ticks;
    public Dictionary<string, object?> Metadata { get; } = new();

    public static AuditExecutionContext Capture()
    {
        var src = Current;
        if (src == null) return new AuditExecutionContext();
        return new AuditExecutionContext
        {
            CorrelationId = src.CorrelationId,
            ActorId = src.ActorId,
            ActorUsername = src.ActorUsername,
            ActorRole = src.ActorRole,
            ActorSource = src.ActorSource,
            Source = src.Source,
            ClientIp = src.ClientIp,
            UserAgent = src.UserAgent,
            HttpMethod = src.HttpMethod,
            HttpPath = src.HttpPath,
            ApiGroup = src.ApiGroup,
            StartTicks = src.StartTicks
        };
    }

    public static IDisposable Scope(AuditExecutionContext ctx)
    {
        var previous = Current;
        Current = ctx;
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly AuditExecutionContext? _previous;
        public RestoreScope(AuditExecutionContext? previous) => _previous = previous;
        public void Dispose() => Current = _previous;
    }
}