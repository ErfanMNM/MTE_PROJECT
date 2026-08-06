using DMS_Project.Audit;
using DMS_Project.Auth;
using DMS_Project.Communications.Orders;
using DMS_Project.Communications.TCP;
using DMS_Project.Config;
using DMS_Project.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using DataPoolService = DMS_Project.DataPool.DataPool;
using ProductionService = DMS_Project.Production.Production;

var builder = WebApplication.CreateBuilder(args);

// Forward-declared service provider holder cho TCP callback runtime access
IServiceProvider? _tcpServiceProvider = null;

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

// ===== Kestrel: listen cả 2 port =====
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    options.ListenAnyIP(49211);
    options.Limits.MaxRequestBodySize = 200_000_000; // 200MB cho upload CSV lớn
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();

// ===== Auth / JWT =====
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

// ===== Audit =====
builder.Services.AddSingleton<AuditRepository>(sp =>
    new AuditRepository(sp.GetRequiredService<AppConfig>().AuditDbPath));
builder.Services.AddSingleton<AuditDbInitializer>(sp =>
    new AuditDbInitializer(sp.GetRequiredService<AppConfig>().AuditDbPath,
        sp.GetRequiredService<ILogger<AuditDbInitializer>>()));
builder.Services.AddSingleton<IAuditService, AuditService>();

// ===== Swagger: 2 doc (main + orders) =====
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

    // JWT bearer cho Swagger UI
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

    // Lọc endpoint theo ApiGroupAttribute cho từng doc
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var group = apiDesc.ActionDescriptor.EndpointMetadata
            .OfType<ApiGroupAttribute>()
            .FirstOrDefault();
        return group != null && group.Name == docName;
    });
});

builder.Services.AddSingleton<DataPoolService>(sp =>
    new DataPoolService(sp.GetRequiredService<IAuditService>()));
builder.Services.AddSingleton<ProductionService>(sp =>
    new ProductionService(sp.GetRequiredService<DataPoolService>(), sp.GetRequiredService<IAuditService>()));
builder.Services.AddSingleton<OrderQueueService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<OrderQueueService>());

// ===== Authentication & Authorization =====
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Service provider phải resolve sau khi Build() mới có instance,
        // nên cấu hình callback post-build: dùng cách đăng ký TokenValidationParameters qua
        // options.PostConfigure để có thể lấy JwtTokenService đã đăng ký singleton.
        options.RequireHttpsMetadata = false;
    });
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtTokenService>((options, jwt) =>
    {
        options.TokenValidationParameters = jwt.BuildValidationParameters();
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ===== Khởi tạo DB: Auth + Audit =====
app.Services.GetRequiredService<AuthDbInitializer>().EnsureCreated();
app.Services.GetRequiredService<AuditDbInitializer>().EnsureCreated();
// Lưu service provider để TCP callback truy cập
_tcpServiceProvider = app.Services;

// ===== Swagger middleware: route doc theo port =====
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "swagger";

    // Doc "orders" dùng riêng cho port 49211
    c.SwaggerEndpoint("/swagger/orders/swagger.json", "DMS Orders API v1");
    c.SwaggerEndpoint("/swagger/main/swagger.json", "DMS Main API v1");
});

// Middleware: trả 404 nếu request vào port không thuộc group của endpoint
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

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Audit middleware: capture HTTP request audit + populate execution context
app.UseMiddleware<AuditMiddleware>();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
});

// Middleware: rewrite URL swagger.json theo port để Swagger UI đúng doc
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/swagger"))
    {
        var localPort = ctx.Connection.LocalPort;
        var groupName = localPort == 49211 ? "orders" : "main";

        // /swagger/v1/swagger.json → /swagger/{group}/swagger.json
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

app.MapControllers();

// Redirect root về swagger
app.MapGet("/", (HttpContext ctx) =>
{
    var url = ctx.Connection.LocalPort == 49211 ? "/swagger" : "/swagger";
    return Results.Redirect(url);
});

app.Run();

// Callback xử lý dữ liệu nhận từ camera
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
            audit?.RecordSuccessAsync(
                action: "Telegram.TcpReceived",
                entityType: AuditEntityTypes.TcpMessage,
                entityId: null,
                before: null,
                after: new
                {
                    length = data?.Length ?? 0,
                    preview = data == null ? null : (data.Length > 200 ? data.Substring(0, 200) + "..." : data),
                    source = "Camera"
                },
                changedFieldsJson: null,
                metadata: new { state });
            break;
        case enumClient.RECONNECT:
            break;
    }
}