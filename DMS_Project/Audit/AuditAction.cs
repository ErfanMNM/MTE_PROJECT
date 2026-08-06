namespace DMS_Project.Audit;

public static class AuditEntityTypes
{
    public const string HttpRequest = "HttpRequest";
    public const string User = "User";
    public const string Pool = "Pool";
    public const string PoolCode = "PoolCode";
    public const string ProductionOrder = "ProductionOrder";
    public const string UniqueCode = "UniqueCode";
    public const string Carton = "Carton";
    public const string Order = "Order";
    public const string Config = "Config";
    public const string TcpMessage = "TcpMessage";
}

public static class AuditOutcomes
{
    public const string Success = "Success";
    public const string Failure = "Failure";
    public const string Partial = "Partial";
    public const string Denied = "Denied";
}

public static class AuditSources
{
    public const string Http = "HTTP";
    public const string QueueWorker = "QueueWorker";
    public const string BackgroundService = "BackgroundService";
    public const string TcpCamera = "TCPCamera";
}

public static class AuditActorSources
{
    public const string Jwt = "JWT";
    public const string System = "System";
    public const string Queue = "Queue";
    public const string Tcp = "TCP";
    public const string Seed = "Seed";
}