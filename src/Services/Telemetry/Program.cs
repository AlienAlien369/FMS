using FMS.MessageBus.Extensions;
using FMS.SharedKernel.Extensions;
using FMS.Telemetry.Data;
using FMS.Telemetry.Services;
using MassTransit;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var serviceName = "Telemetry Service";

// ── Shared Kernel ──
builder.Services.AddFmsSharedKernel(config, serviceName);

// ── MongoDB ──
var mongoClient = new MongoClient(config["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017");
var mongoDb = mongoClient.GetDatabase(config["MongoDb:DatabaseName"] ?? "fms_telemetry");
builder.Services.AddSingleton<IMongoDatabase>(mongoDb);
builder.Services.AddSingleton<ITelemetryRepository, TelemetryRepository>();

// ── MassTransit (consume telemetry events) ──
builder.Services.AddFmsMessageBus(config, serviceName, x =>
{
    x.AddConsumer<TelemetryEventConsumer>();
    x.AddConsumer<AlertEventConsumer>();
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseFmsSharedKernel(app.Services.GetRequiredService<IHostEnvironment>());

// Create MongoDB indexes
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<ITelemetryRepository>();
    await repo.EnsureIndexesAsync();
}

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("📡 Telemetry Service started on {Urls}", config["Urls"] ?? "http://localhost:5300");
});

app.Run();
