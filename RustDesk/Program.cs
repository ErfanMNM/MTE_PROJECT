using RustDesk;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RustDeskSettings>(builder.Configuration.GetSection("RustDesk"));
builder.Services.AddHostedService<Worker>();

// Chạy như Windows Service hoặc Console (tùy environment)
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService();
}

var host = builder.Build();
host.Run();
