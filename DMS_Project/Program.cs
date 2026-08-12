using DMS_Project.Communications.WebSockets.Server;

var builder = WebApplication.CreateBuilder(args);

// Cổng + bind tất cả interface
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Đăng ký WS connection manager (singleton)
builder.Services.AddSingleton<WsConnectionManager>();

// CORS — cần thiết cho browser khi gọi từ origin khác
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin()       // dev; production nên giới hạn lại
        .WithOrigins("http://localhost:3000", "http://localhost:5173")); // các origin được phép
});

builder.Services.AddLogging(cfg =>
{
    cfg.AddConsole();
    cfg.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

app.UseCors();

// Bật WebSocket middleware
var wsOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15), // gửi ping protocol mỗi 15s
    // ReceiveBufferSize mặc định 4KB đã đủ, không cần set
};
app.UseWebSockets(wsOptions);

// Endpoint WS
app.Map("/ws", async context =>
{
    // Origin check — chỉ chấp nhận origin được phép
    var origin = context.Request.Headers["Origin"].ToString();
    var allowed = new[] { "http://localhost:3000", "http://localhost:5173", "" };
    if (!string.IsNullOrEmpty(origin) && !allowed.Contains(origin))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    var manager = context.RequestServices.GetRequiredService<WsConnectionManager>();
    await manager.HandleAsync(context);
});

// Health check
app.MapGet("/", () => Results.Ok(new
{
    service = "DMS_Project WS Server",
    ws = "/ws",
    status = "running",
    time = DateTimeOffset.UtcNow,
}));

app.MapGet("/stats", (WsConnectionManager mgr) => Results.Ok(new
{
    connections = mgr.ConnectionCount,
    time = DateTimeOffset.UtcNow,
}));

Console.WriteLine("[WS] Server listening on http://0.0.0.0:5000/ws");
Console.WriteLine("[WS] KeepAliveInterval = 15s");
Console.WriteLine("[WS] Allowed origins: localhost:3000, localhost:5173");

await app.RunAsync();
