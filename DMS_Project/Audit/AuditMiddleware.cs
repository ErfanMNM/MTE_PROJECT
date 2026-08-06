using System.Diagnostics;
using DMS_Project.Infrastructure;

namespace DMS_Project.Audit;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditService _audit;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, IAuditService audit, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _audit = audit;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Bỏ qua các path infra: swagger, root redirect, health probe để tránh spam audit
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path == "/" ||
            path == "/favicon.ico")
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString();

        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var actorSource = AuditActorSources.System;
        string actorUsername = "anonymous";
        string actorRole = "Anonymous";
        int? actorId = null;

        if (context.User?.Identity?.IsAuthenticated == true)
        {
            actorSource = AuditActorSources.Jwt;
            actorUsername = context.User.Identity.Name ?? "unknown";
            actorRole = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Anonymous";
            var sub = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (int.TryParse(sub, out var idParsed)) actorId = idParsed;
        }

        var apiGroup = context.GetEndpoint()?.Metadata.GetMetadata<ApiGroupAttribute>()?.Name;

        var ctx = new AuditExecutionContext
        {
            CorrelationId = correlationId,
            ActorId = actorId,
            ActorUsername = actorUsername,
            ActorRole = actorRole,
            ActorSource = actorSource,
            Source = AuditSources.Http,
            ClientIp = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.FirstOrDefault(),
            HttpMethod = context.Request.Method,
            HttpPath = path + context.Request.QueryString,
            ApiGroup = apiGroup,
            StartTicks = DateTime.UtcNow.Ticks
        };

        using (AuditExecutionContext.Scope(ctx))
        {
            Exception? caught = null;
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                caught = ex;
                throw;
            }
            finally
            {
                sw.Stop();
                var statusCode = context.Response.StatusCode;
                var outcome = statusCode >= 400 ? AuditOutcomes.Failure : AuditOutcomes.Success;
                if (caught != null) outcome = AuditOutcomes.Failure;

                var metadata = new Dictionary<string, object?>
                {
                    ["contentLength"] = context.Response.ContentLength,
                    ["queryString"] = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null
                };
                if (caught != null) metadata["exception"] = caught.GetType().Name + ": " + caught.Message;

                await _audit.RecordSuccessAsync(
                    action: "Http.Request",
                    entityType: AuditEntityTypes.HttpRequest,
                    entityId: correlationId,
                    before: null,
                    after: new { statusCode, durationMs = (int)sw.ElapsedMilliseconds },
                    changedFieldsJson: null,
                    parentEntityType: null,
                    parentEntityId: null,
                    metadata: metadata);
            }
        }
    }
}