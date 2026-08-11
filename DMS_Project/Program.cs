#region === USINGS ===
using DMS_Project.Audit;
using DMS_Project.Auth;
using DMS_Project.Communications.Orders;
using DMS_Project.Communications.TCP;
using DMS_Project.Config;
using DMS_Project.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using DataPoolService = DMS_Project.DataPool.DataPool;
using ProductionService = DMS_Project.Production.Production;
#endregion

#region === BUILDER CONFIGURATION ===
var builder = WebApplication.CreateBuilder(args);

IServiceProvider? _tcpServiceProvider = null;
int _isBusy = 0;

AppConfig appConfig = new AppConfig();
appConfig = ConfigStorage.Load<AppConfig>();
builder.Services.AddSingleton(appConfig);

TCPClient tcpCamera = new TCPClient
{
    IP = appConfig.Camera_Ip ?? "127.0.0.1",
    Port = appConfig.Camera_Port
};

tcpCamera.ClientCallBack += (state, data) =>
{
    try { TcpCamera_ClientCallBack(state, data); } catch { /* do not crash TCP loop */ }
};

tcpCamera.Connect();
#endregion

#region === KESTREL CONFIG ===
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    options.ListenAnyIP(49211);
    options.ListenAnyIP(51883);
    options.Limits.MaxRequestBodySize = 200_000_000;
});
#endregion

#region === MVC & SWAGGER ===
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("main", new()
    {
        Title = "DMS Main API",
        Version = "v1",
        Description = "REST API cho DataPool & Production (port 5000)"
    });
    c.SwaggerDoc("orders", new()
    {
        Title = "DMS Orders API",
        Version = "v1",
        Description = "REST API cho Orders (port 49211)"
    });

    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new()
    {
        [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var group = apiDesc.ActionDescriptor.EndpointMetadata
            .OfType<ApiGroupAttribute>()
            .FirstOrDefault();
        return group != null && group.Name == docName;
    });
});
#endregion

#region === SERVICES ===
builder.Services.AddSingleton<AuthRepository>(sp =>
    new AuthRepository(sp.GetRequiredService<AppConfig>().AuthDbPath));
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<JwtTokenService>(sp =>
{
    var cfg = sp.GetRequiredService<AppConfig>();
    return new JwtTokenService(cfg.JwtSecret, cfg.JwtIssuer, cfg.JwtAudience, cfg.JwtExpirationMinutes);
});
builder.Services.AddSingleton<AuthDbInitializer>();
builder.Services.AddSingleton<IAuthService, AuthService>();

builder.Services.AddSingleton<AuditRepository>(sp =>
    new AuditRepository(sp.GetRequiredService<AppConfig>().AuditDbPath));
builder.Services.AddSingleton<AuditDbInitializer>(sp =>
    new AuditDbInitializer(sp.GetRequiredService<AppConfig>().AuditDbPath,
        sp.GetRequiredService<ILogger<AuditDbInitializer>>()));
builder.Services.AddSingleton<IAuditService, AuditService>();

builder.Services.AddSingleton<DataPoolService>(sp =>
    new DataPoolService(sp.GetRequiredService<IAuditService>()));
builder.Services.AddSingleton<ProductionService>(sp =>
    new ProductionService(sp.GetRequiredService<DataPoolService>(), sp.GetRequiredService<IAuditService>()));
builder.Services.AddSingleton<OrderQueueService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderQueueService>());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
    });
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtTokenService>((options, jwt) =>
    {
        options.TokenValidationParameters = jwt.BuildValidationParameters();
    });
builder.Services.AddAuthorization();
#endregion

var app = builder.Build();

#region === APP STARTUP ===
app.Services.GetRequiredService<AuthDbInitializer>().EnsureCreated();
app.Services.GetRequiredService<AuditDbInitializer>().EnsureCreated();
_tcpServiceProvider = app.Services;
#endregion

#region === MIDDLEWARE ===
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";
    c.SwaggerEndpoint("/swagger/orders/swagger.json", "DMS Orders API v1");
    c.SwaggerEndpoint("/swagger/main/swagger.json", "DMS Main API v1");
});

app.Use(async (ctx, next) =>
{
    var endpoint = ctx.GetEndpoint();
    if (endpoint != null)
    {
        var group = endpoint.Metadata.GetMetadata<ApiGroupAttribute>();
        if (group != null)
        {
            var localPort = ctx.Connection.LocalPort;
            var expectedPort = group.Name == "orders" ? 49211 : 5000;
            if (localPort != expectedPort)
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
        }
    }
    await next();
});

app.UseWebSockets();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditMiddleware>();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
});

app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/swagger"))
    {
        var localPort = ctx.Connection.LocalPort;
        var groupName = localPort == 49211 ? "orders" : "main";

        var originalPath = ctx.Request.Path.Value ?? string.Empty;
        if (originalPath.Equals("/swagger/v1/swagger.json", StringComparison.OrdinalIgnoreCase) ||
            originalPath.Equals("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Request.Path = localPort == 49211
                ? "/swagger/orders/swagger.json"
                : "/swagger/main/swagger.json";
        }
    }
    await next();
});
#endregion

#region === ENDPOINTS ===
app.MapControllers();

app.Map("/ws/c1", async (HttpContext context) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = new DMS_Project.Communications.WebSockets.C1WebSocketHandler(webSocket);
        await handler.HandleAsync();
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.MapGet("/", (HttpContext ctx) =>
{
    var url = ctx.Connection.LocalPort == 49211 ? "/swagger" : "/swagger";
    return Results.Redirect(url);
});

app.Run();
#endregion

#region === CAMERA CALLBACK ===
void TcpCamera_ClientCallBack(enumClient state, string data)
{
    var audit = _tcpServiceProvider?.GetService<IAuditService>();
    switch (state)
    {
        case enumClient.CONNECTED:
            break;
        case enumClient.DISCONNECTED:
            break;
        case enumClient.RECEIVED:
            if (Interlocked.CompareExchange(ref _isBusy, 1, 0) == 0)
            {
                tcpCamera.Send("BUSY");
                _ = Task.Run(() =>
                {
                    try
                    {
                        HandleCameraData(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CAMERA ERROR] {ex.Message}");
                    }
                    finally
                    {
                        Interlocked.Exchange(ref _isBusy, 0);
                        tcpCamera.Send("READY");
                    }
                });
            }
            else
            {
                tcpCamera.Send("BUSY");
            }
            break;
        case enumClient.RECONNECT:
            break;
    }
}

void HandleCameraData(string data)
{
    // TODO: Xử lý data từ camera
    Console.WriteLine($"[CAMERA] Received: {data}");
}
#endregion

#region === BACKGROUND SERVICES ===
public class AppStateService : BackgroundService
{
    private readonly ILogger<AppStateService> _logger;

    public AppStateService(ILogger<AppStateService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AppStateService error");
            }

            await Task.Delay(100, stoppingToken);
        }
    }

    private void DoWork()
    {
        // TODO: Logic của bạn
    }
}
#endregion
