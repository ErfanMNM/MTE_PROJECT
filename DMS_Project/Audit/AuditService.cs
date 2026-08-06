using System.Threading.Channels;

namespace DMS_Project.Audit;

public interface IAuditService
{
    Task RecordSuccessAsync(string action, string entityType, string? entityId,
        object? before, object? after, string? changedFieldsJson,
        string? parentEntityType = null, string? parentEntityId = null,
        object? metadata = null,
        CancellationToken ct = default);

    Task RecordFailureAsync(string action, string entityType, string? entityId,
        string error, object? before = null,
        string? parentEntityType = null, string? parentEntityId = null,
        object? metadata = null,
        CancellationToken ct = default);
}

public class AuditService : IAuditService, IDisposable
{
    private readonly AuditRepository _repo;
    private readonly Channel<AuditEvent> _queue;
    private readonly Task _worker;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger<AuditService> _logger;
    private const int MaxQueueSize = 10_000;

    public AuditService(AuditRepository repo, ILogger<AuditService> logger)
    {
        _repo = repo;
        _logger = logger;
        _queue = Channel.CreateBounded<AuditEvent>(new BoundedChannelOptions(MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });
        _worker = Task.Run(FlushLoopAsync);
    }

    public Task RecordSuccessAsync(string action, string entityType, string? entityId,
        object? before, object? after, string? changedFieldsJson,
        string? parentEntityType = null, string? parentEntityId = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var evt = BuildEvent(action, entityType, entityId, AuditOutcomes.Success,
            parentEntityType, parentEntityId, metadata);
        evt.BeforeJson = AuditEnricher.ToJson(before);
        evt.AfterJson = AuditEnricher.ToJson(after);
        evt.ChangedFields = changedFieldsJson;
        Enqueue(evt);
        return Task.CompletedTask;
    }

    public Task RecordFailureAsync(string action, string entityType, string? entityId,
        string error, object? before = null,
        string? parentEntityType = null, string? parentEntityId = null,
        object? metadata = null,
        CancellationToken ct = default)
    {
        var evt = BuildEvent(action, entityType, entityId, AuditOutcomes.Failure,
            parentEntityType, parentEntityId, metadata);
        evt.ErrorMessage = error;
        evt.BeforeJson = AuditEnricher.ToJson(before);
        Enqueue(evt);
        return Task.CompletedTask;
    }

    private AuditEvent BuildEvent(string action, string entityType, string? entityId,
        string outcome, string? parentEntityType, string? parentEntityId, object? metadata)
    {
        var ctx = AuditExecutionContext.Current;
        var evt = new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            TimestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ActorId = ctx?.ActorId,
            ActorUsername = ctx?.ActorUsername ?? "system",
            ActorRole = ctx?.ActorRole ?? "Anonymous",
            ActorSource = ctx?.ActorSource ?? AuditActorSources.System,
            CorrelationId = ctx?.CorrelationId ?? Guid.NewGuid().ToString(),
            Source = ctx?.Source ?? AuditSources.Http,
            HttpMethod = ctx?.HttpMethod,
            HttpPath = ctx?.HttpPath,
            ClientIp = ctx?.ClientIp,
            UserAgent = ctx?.UserAgent,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ParentEntityType = parentEntityType,
            ParentEntityId = parentEntityId,
            Outcome = outcome,
            ApiGroup = ctx?.ApiGroup,
            MetadataJson = AuditEnricher.ToJson(metadata)
        };

        if (ctx != null)
        {
            var elapsed = (DateTime.UtcNow.Ticks - ctx.StartTicks) / TimeSpan.TicksPerMillisecond;
            if (elapsed > 0) evt.DurationMs = (int)elapsed;
        }
        return evt;
    }

    private void Enqueue(AuditEvent evt)
    {
        // Insert synchronously để đảm bảo không mất event khi background worker bị block.
        // Trade-off: thêm 1-5ms mỗi audit call - acceptable cho hệ thống internal.
        try
        {
            _repo.Insert(evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit event {EventId} action {Action}", evt.EventId, evt.Action);
        }
    }

    private async Task FlushLoopAsync()
    {
        var batch = new List<AuditEvent>(64);
        try
        {
            await foreach (var evt in _queue.Reader.ReadAllAsync(_cts.Token))
            {
                batch.Add(evt);
                if (batch.Count >= 32) Flush(batch);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (batch.Count > 0) Flush(batch);
        }
    }

    private void Flush(List<AuditEvent> events)
    {
        foreach (var evt in events)
        {
            try
            {
                _repo.Insert(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit event {EventId} action {Action}", evt.EventId, evt.Action);
            }
        }
        events.Clear();
    }

    public void Dispose()
    {
        _queue.Writer.TryComplete();
        _cts.Cancel();
        try { _worker.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts.Dispose();
    }
}