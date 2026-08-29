using FMS.Auth.Data;
using FMS.Auth.Services;
using FMS.SharedKernel.Extensions;
using FMS.SharedKernel.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var serviceName = "Auth Service";

// ── Shared Kernel (logging, telemetry, health, JWT, Swagger) ──
builder.Services.AddFmsSharedKernel(config, serviceName);

// ── PostgreSQL ──
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

// ── Redis (refresh token store) ──
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = config["Redis:ConnectionString"] ?? "localhost:6379";
    options.InstanceName = "fms:auth:";
});

// ── Services ──
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseFmsSharedKernel(app.Services.GetRequiredService<IHostEnvironment>());

// ── Auto-migrate ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("🔐 Auth Service started on {Urls}", config["Urls"] ?? "http://localhost:5100");
});

app.Run();
