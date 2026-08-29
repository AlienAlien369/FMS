using FMS.Entity.Data;
using FMS.SharedKernel.Extensions;
using FMS.SharedKernel.Middleware;
using FMS.SharedKernel.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var serviceName = "Entity Service";

// ── Shared Kernel ──
builder.Services.AddFmsSharedKernel(config, serviceName);

// ── PostgreSQL ──
builder.Services.AddDbContext<EntityDbContext>(options =>
    options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

// ── CQRS ──
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddControllers();

var app = builder.Build();

app.UseFmsSharedKernel(app.Services.GetRequiredService<IHostEnvironment>());

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EntityDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("🚛 Entity Service started on {Urls}", config["Urls"] ?? "http://localhost:5200");
});

app.Run();
