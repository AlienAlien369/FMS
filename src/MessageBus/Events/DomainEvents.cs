namespace FMS.MessageBus.Events;

// ──────────────────────────────────────
// Fleet Events
// ──────────────────────────────────────

public sealed record VehicleCreatedEvent
{
    public Guid VehicleId { get; init; }
    public Guid TenantId { get; init; }
    public string VehicleNumber { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Model { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed record VehicleStatusChangedEvent
{
    public Guid VehicleId { get; init; }
    public Guid TenantId { get; init; }
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed record DriverCreatedEvent
{
    public Guid DriverId { get; init; }
    public Guid TenantId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public sealed record DriverScoreChangedEvent
{
    public Guid DriverId { get; init; }
    public Guid TenantId { get; init; }
    public decimal OldScore { get; init; }
    public decimal NewScore { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

// ──────────────────────────────────────
// Device / IoT Events
// ──────────────────────────────────────

public sealed record DeviceTelemetryEvent
{
    public Guid DeviceId { get; init; }
    public Guid TenantId { get; init; }
    public string VendorCode { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? Speed { get; init; }
    public double? Heading { get; init; }
    public bool? Ignition { get; init; }
    public double? Odometer { get; init; }
    public double? FuelLevel { get; init; }
    public double? Temperature { get; init; }
    public Dictionary<string, object>? Alerts { get; init; }
    public Dictionary<string, object>? RawPayload { get; init; }
}

public sealed record DeviceCommandSentEvent
{
    public Guid CommandId { get; init; }
    public Guid DeviceId { get; init; }
    public Guid TenantId { get; init; }
    public string CommandType { get; init; } = string.Empty;
    public DateTime SentAt { get; init; } = DateTime.UtcNow;
}

public sealed record DeviceCommandAckedEvent
{
    public Guid CommandId { get; init; }
    public Guid DeviceId { get; init; }
    public Guid TenantId { get; init; }
    public string CommandType { get; init; } = string.Empty;
    public Dictionary<string, object>? ResponsePayload { get; init; }
    public DateTime AckedAt { get; init; } = DateTime.UtcNow;
}

public sealed record DeviceOfflineEvent
{
    public Guid DeviceId { get; init; }
    public Guid TenantId { get; init; }
    public DateTime LastSeen { get; init; }
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

// ──────────────────────────────────────
// Alert Events
// ──────────────────────────────────────

public sealed record AlertTriggeredEvent
{
    public Guid AlertId { get; init; }
    public Guid TenantId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public Guid? DeviceId { get; init; }
    public string AlertType { get; init; } = string.Empty;   // overspeed, geofence-breach, panic, fuel-theft, harsh-driving
    public string Severity { get; init; } = string.Empty;     // critical, high, medium, low
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? Speed { get; init; }
    public double? Threshold { get; init; }
    public string? Message { get; init; }
    public DateTime TriggeredAt { get; init; } = DateTime.UtcNow;
}

public sealed record AlertResolvedEvent
{
    public Guid AlertId { get; init; }
    public Guid TenantId { get; init; }
    public Guid? ResolvedByUserId { get; init; }
    public string? Notes { get; init; }
    public DateTime ResolvedAt { get; init; } = DateTime.UtcNow;
}

// ──────────────────────────────────────
// Trip Events
// ──────────────────────────────────────

public sealed record TripStartedEvent
{
    public Guid TripId { get; init; }
    public Guid TenantId { get; init; }
    public Guid VehicleId { get; init; }
    public Guid? DriverId { get; init; }
    public DateTime StartTime { get; init; }
    public string? Origin { get; init; }
    public double? OriginLat { get; init; }
    public double? OriginLng { get; init; }
}

public sealed record TripEndedEvent
{
    public Guid TripId { get; init; }
    public Guid TenantId { get; init; }
    public Guid VehicleId { get; init; }
    public DateTime EndTime { get; init; }
    public double TotalDistanceKm { get; init; }
    public double TotalFuelConsumed { get; init; }
    public int DeviationCount { get; init; }
}

public sealed record TripDeviationEvent
{
    public Guid TripId { get; init; }
    public Guid TenantId { get; init; }
    public string DeviationType { get; init; } = string.Empty; // route-deviation, unauthorized-stop, overspeed
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

// ──────────────────────────────────────
// Notification Events
// ──────────────────────────────────────

public sealed record SendNotificationEvent
{
    public Guid NotificationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid? UserId { get; init; }
    public string Channel { get; init; } = string.Empty; // email, sms, push, whatsapp
    public string TemplateKey { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public Dictionary<string, string>? TemplateData { get; init; }
    public DateTime ScheduledAt { get; init; } = DateTime.UtcNow;
}

// ──────────────────────────────────────
// Tenant Events
// ──────────────────────────────────────

public sealed record TenantOnboardedEvent
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string Subdomain { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string Plan { get; init; } = string.Empty;
    public string AdminEmail { get; init; } = string.Empty;
    public List<string> EnabledModules { get; init; } = new();
    public DateTime OnboardedAt { get; init; } = DateTime.UtcNow;
}

public sealed record TenantSubscriptionChangedEvent
{
    public Guid TenantId { get; init; }
    public string OldPlan { get; init; } = string.Empty;
    public string NewPlan { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
}
