using FMS.SharedKernel.Extensions;
using FMS.SharedKernel.Middleware;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var serviceName = "API Gateway";

// ── Shared Kernel (logging, telemetry, health) ──
builder.Services.AddFmsSharedKernel(config, serviceName);

// ── Ocelot ──
builder.Services.AddOcelot();

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseFmsSharedKernel(app.Services.GetRequiredService<IHostEnvironment>());

// ── Ocelot Pipeline ──
await app.UseOcelot();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("🌐 API Gateway started on {Urls}", config["Urls"] ?? "http://localhost:5000");
});

app.Run();
