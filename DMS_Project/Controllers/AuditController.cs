using System.Globalization;
using System.Text;
using System.Text.Json;
using DMS_Project.Auth;
using DMS_Project.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMS_Project.Controllers;

[ApiController]
[Route("api/audit")]
[Produces("application/json")]
[ApiGroup("main")]
public class AuditController : ControllerBase
{
    private readonly Audit.AuditRepository _repo;

    public AuditController(Audit.AuditRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Operator}")]
    [ProducesResponseType(typeof(Audit.PagedAuditDto), StatusCodes.Status200OK)]
    public IActionResult Query([FromQuery] Audit.AuditQuery query)
    {
        var result = _repo.Query(query);
        return Ok(new ApiResponse<Audit.PagedAuditDto>
        {
            Success = true,
            Message = "OK",
            Data = result
        });
    }

    [HttpGet("{eventId}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Operator}")]
    [ProducesResponseType(typeof(Audit.AuditEvent), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Get(string eventId)
    {
        var evt = _repo.FindByEventId(eventId);
        if (evt == null) return NotFound(new { message = "Không tìm thấy event" });
        return Ok(evt);
    }

    [HttpGet("export")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Export([FromQuery] Audit.AuditExportRequest req)
    {
        var format = (req?.Format ?? "csv").ToLowerInvariant();
        var auditQuery = new Audit.AuditQuery
        {
            FromUtc = req?.FromUtc,
            ToUtc = req?.ToUtc,
            EntityType = req?.EntityType,
            EntityId = req?.EntityId,
            ActorUsername = req?.ActorUsername,
            Action = req?.Action,
            Outcome = req?.Outcome,
            Source = req?.Source
        };

        var maxRows = req?.MaxRows is > 0 and <= 1_000_000 ? req.MaxRows : 100_000;
        var fromForName = req?.FromUtc ?? DateTime.UtcNow.AddDays(-7);
        var toForName = req?.ToUtc ?? DateTime.UtcNow;

        string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var needsQuote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            var escaped = value.Replace("\"", "\"\"");
            return needsQuote ? $"\"{escaped}\"" : escaped;
        }

        string BuildFileName(string ext)
        {
            var from = fromForName.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var to = toForName.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            return $"audit-export-{from}-{to}.{ext}";
        }

        if (format == "jsonl")
        {
            var ms = new MemoryStream();
            using (var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                foreach (var evt in _repo.Stream(auditQuery, maxRows))
                {
                    writer.WriteLine(JsonSerializer.Serialize(evt, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
                }
            }
            ms.Position = 0;
            return File(ms, "application/x-ndjson", BuildFileName("jsonl"));
        }

        // default CSV
        var csvMs = new MemoryStream();
        using (var writer = new StreamWriter(csvMs, Encoding.UTF8, leaveOpen: true))
        {
            writer.WriteLine(string.Join(",", new[]
            {
                "EventId","TimestampUtc","ActorId","ActorUsername","ActorRole","ActorSource",
                "CorrelationId","Source","HttpMethod","HttpPath","HttpStatusCode","ClientIp","UserAgent",
                "Action","EntityType","EntityId","ParentEntityType","ParentEntityId",
                "Outcome","ErrorMessage","ChangedFields","ApiGroup","DurationMs",
                "BeforeJson","AfterJson","MetadataJson"
            }));
            foreach (var evt in _repo.Stream(auditQuery, maxRows))
            {
                writer.WriteLine(string.Join(",", new[]
                {
                    Csv(evt.EventId),
                    Csv(evt.TimestampUtc),
                    evt.ActorId?.ToString() ?? "",
                    Csv(evt.ActorUsername),
                    Csv(evt.ActorRole),
                    Csv(evt.ActorSource),
                    Csv(evt.CorrelationId),
                    Csv(evt.Source),
                    Csv(evt.HttpMethod ?? ""),
                    Csv(evt.HttpPath ?? ""),
                    evt.HttpStatusCode?.ToString() ?? "",
                    Csv(evt.ClientIp ?? ""),
                    Csv(evt.UserAgent ?? ""),
                    Csv(evt.Action),
                    Csv(evt.EntityType),
                    Csv(evt.EntityId ?? ""),
                    Csv(evt.ParentEntityType ?? ""),
                    Csv(evt.ParentEntityId ?? ""),
                    Csv(evt.Outcome),
                    Csv(evt.ErrorMessage ?? ""),
                    Csv(evt.ChangedFields ?? ""),
                    Csv(evt.ApiGroup ?? ""),
                    evt.DurationMs?.ToString() ?? "",
                    Csv(evt.BeforeJson ?? ""),
                    Csv(evt.AfterJson ?? ""),
                    Csv(evt.MetadataJson ?? "")
                }));
            }
        }
        csvMs.Position = 0;
        return File(csvMs, "text/csv", BuildFileName("csv"));
    }
}