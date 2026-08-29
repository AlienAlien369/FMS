using FMS.MessageBus.Events;
using FMS.Telemetry.Data;
using MassTransit;
using MongoDB.Bson;

namespace FMS.Telemetry.Services;

public class TelemetryEventConsumer : IConsumer<DeviceTelemetryEvent>
{
    private readonly ITelemetryRepository _repository;
    private readonly ILogger<TelemetryEventConsumer> _logger;

    public TelemetryEventConsumer(ITelemetryRepository repository, ILogger<TelemetryEventConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeviceTelemetryEvent> context)
    {
        var msg = context.Message;

        _logger.LogDebug("Storing telemetry for device {DeviceId} at {Timestamp}",
            msg.DeviceId, msg.Timestamp);

        var location = new BsonDocument
        {
            { "type", "Point" },
            { "coordinates", new BsonArray(new double[] { msg.Longitude, msg.Latitude }) }
        };

        var record = new DeviceRecord
        {
            TenantId = msg.TenantId,
            DeviceId = msg.DeviceId,
            VendorCode = msg.VendorCode,
            Timestamp = msg.Timestamp,
            Location = location,
            Speed = msg.Speed,
            Heading = msg.Heading,
            Ignition = msg.Ignition,
            Odometer = msg.Odometer,
            FuelLevel = msg.FuelLevel,
            Temperature = msg.Temperature
        };

        await _repository.InsertDeviceRecordAsync(record);

        // Check for overspeed and trigger alert
        if (msg.Speed.HasValue && msg.Speed > 120)
        {
            await context.Publish(new AlertTriggeredEvent
            {
                AlertId = Guid.NewGuid(),
                TenantId = msg.TenantId,
                DeviceId = msg.DeviceId,
                AlertType = "overspeed",
                Severity = msg.Speed > 140 ? "critical" : "high",
                Latitude = msg.Latitude,
                Longitude = msg.Longitude,
                Speed = msg.Speed,
                Threshold = 120,
                Message = $"Vehicle overspeeding at {msg.Speed} km/h (limit: 120 km/h)",
                TriggeredAt = msg.Timestamp
            });
        }

        if (msg.FuelLevel.HasValue && msg.FuelLevel < 10)
        {
            await context.Publish(new AlertTriggeredEvent
            {
                AlertId = Guid.NewGuid(),
                TenantId = msg.TenantId,
                DeviceId = msg.DeviceId,
                AlertType = "low_fuel",
                Severity = msg.FuelLevel < 5 ? "critical" : "medium",
                Latitude = msg.Latitude,
                Longitude = msg.Longitude,
                Threshold = 10,
                Message = $"Low fuel level: {msg.FuelLevel}%",
                TriggeredAt = msg.Timestamp
            });
        }
    }
}

public class AlertEventConsumer : IConsumer<AlertTriggeredEvent>
{
    private readonly ITelemetryRepository _repository;
    private readonly ILogger<AlertEventConsumer> _logger;

    public AlertEventConsumer(ITelemetryRepository repository, ILogger<AlertEventConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AlertTriggeredEvent> context)
    {
        var msg = context.Message;

        _logger.LogWarning("Alert triggered: {AlertType} [{Severity}] for device {DeviceId}",
            msg.AlertType, msg.Severity, msg.DeviceId);

        BsonDocument? location = null;
        if (msg.Latitude.HasValue && msg.Longitude.HasValue)
        {
            location = new BsonDocument
            {
                { "type", "Point" },
                { "coordinates", new BsonArray(new double[] { msg.Longitude.Value, msg.Latitude.Value }) }
            };
        }

        var record = new AlertRecord
        {
            AlertId = msg.AlertId,
            TenantId = msg.TenantId,
            VehicleId = msg.VehicleId,
            DriverId = msg.DriverId,
            DeviceId = msg.DeviceId,
            AlertType = msg.AlertType,
            Severity = msg.Severity,
            Location = location,
            Speed = msg.Speed,
            Threshold = msg.Threshold,
            Message = msg.Message,
            TriggeredAt = msg.TriggeredAt
        };

        await _repository.InsertAlertAsync(record);

        if (msg.Severity is "critical" or "high")
        {
            await context.Publish(new SendNotificationEvent
            {
                NotificationId = Guid.NewGuid(),
                TenantId = msg.TenantId,
                Channel = "push",
                TemplateKey = $"alert_{msg.AlertType}",
                Recipient = "admin",
                TemplateData = new Dictionary<string, string>
                {
                    ["alertType"] = msg.AlertType,
                    ["severity"] = msg.Severity,
                    ["message"] = msg.Message ?? "",
                    ["speed"] = msg.Speed?.ToString("F1") ?? "N/A"
                }
            });
        }
    }
}
