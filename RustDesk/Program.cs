using RustDesk;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RustDeskSettings>(builder.Configuration.GetSection("RustDesk"));
builder.Services.AddHostedService<Worker>();
builder.Services.AddWindowsService();

var host = builder.Build();
host.Run();
