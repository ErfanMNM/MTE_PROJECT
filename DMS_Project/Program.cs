using DMS_Project.DataPool;
using DMS_Project.Production;
using DataPoolService = DMS_Project.DataPool.DataPool;
using ProductionService = DMS_Project.Production.Production;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
});

// Add services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Add CORS
builder.Services.AddCors();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "DMS API", 
        Version = "v1",
        Description = "REST API for DMS Project - DataPool & Production"
    });
});

// Register DataPool as singleton
builder.Services.AddSingleton<DataPoolService>();

// Register Production as singleton (depends on DataPool)
builder.Services.AddSingleton<ProductionService>(sp => 
    new ProductionService(sp.GetRequiredService<DataPoolService>()));

var app = builder.Build();

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DMS API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

// Configure CORS for frontend
app.UseCors(policy =>
{
    policy.WithOrigins("http://localhost:65520", "http://127.0.0.1:65520")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
});

app.MapControllers();

// Health check endpoint
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
