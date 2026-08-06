namespace DMS_Project.Audit;

public class AuditEvent
{
    public long Id { get; set; }
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string TimestampUtc { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

    public int? ActorId { get; set; }
    public string ActorUsername { get; set; } = "system";
    public string ActorRole { get; set; } = "Anonymous";
    public string ActorSource { get; set; } = AuditActorSources.System;

    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string Source { get; set; } = AuditSources.Http;

    public string? HttpMethod { get; set; }
    public string? HttpPath { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? ParentEntityType { get; set; }
    public string? ParentEntityId { get; set; }

    public string Outcome { get; set; } = AuditOutcomes.Success;
    public string? ErrorMessage { get; set; }

    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? ChangedFields { get; set; }
    public string? MetadataJson { get; set; }

    public string? ApiGroup { get; set; }
    public int? DurationMs { get; set; }
}

public class AuditQuery
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? ActorUsername { get; set; }
    public string? Action { get; set; }
    public string? Outcome { get; set; }
    public string? Source { get; set; }
    public string? CorrelationId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class PagedAuditDto
{
    public List<AuditEvent> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

public class AuditExportRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? ActorUsername { get; set; }
    public string? Action { get; set; }
    public string? Outcome { get; set; }
    public string? Source { get; set; }
    public string Format { get; set; } = "csv";
    public int MaxRows { get; set; } = 1_000_000;
}