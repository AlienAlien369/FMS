using FMS.DataCollector.Services;
using FMS.MessageBus.Extensions;
using FMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var serviceName = "DataCollector Service";

builder.Services.AddFmsSharedKernel(config, serviceName);

// ── MQTT Client ──
builder.Services.AddSingleton<IMqttService, MqttService>();
builder.Services.AddSingleton<IDeviceAdapterRegistry, DeviceAdapterRegistry>();

// ── MassTransit (publish telemetry events) ──
builder.Services.AddFmsMessageBus(config, serviceName);

// ── Background MQTT listener ──
builder.Services.AddHostedService<MqttListenerService>();

var app = builder.Build();
app.UseFmsSharedKernel(app.Services.GetRequiredService<IHostEnvironment>());
app.MapControllers();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Services.GetRequiredService<ILogger<Program>>()
        .LogInformation("📡 DataCollector Service started on {Urls}", config["Urls"] ?? "http://localhost:5600");
});

app.Run();
