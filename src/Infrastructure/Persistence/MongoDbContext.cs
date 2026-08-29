using MongoDB.Driver;

namespace FMS.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }

    // Collections
    public IMongoCollection<TelemetryDocument> Telemetry => GetCollection<TelemetryDocument>("telemetry");
    public IMongoCollection<TripDocument> Trips => GetCollection<TripDocument>("trips");
    public IMongoCollection<AlertDocument> Alerts => GetCollection<AlertDocument>("alerts");
    public IMongoCollection<VideoEventDocument> VideoEvents => GetCollection<VideoEventDocument>("video_events");
}

// MongoDB document models
public class TelemetryDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public GeoLocation? Location { get; set; }
    public decimal? Speed { get; set; }
    public decimal? Heading { get; set; }
    public bool? Ignition { get; set; }
    public decimal? Odometer { get; set; }
    public decimal? FuelLevel { get; set; }
    public decimal? Temperature { get; set; }
    public List<AlertInfo>? Alerts { get; set; }
    public Dictionary<string, object>? RawPayload { get; set; }
    public bool Normalized { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class TripDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string TripId { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public List<RoutePoint>? Route { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AlertDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public Guid DriverId { get; set; }
    public GeoLocation? Location { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Resolved { get; set; }
}

public class VideoEventDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public GeoLocation? TriggerLocation { get; set; }
    public List<VideoClip>? Clips { get; set; }
}

// Supporting types
public class GeoLocation
{
    public string Type { get; set; } = "Point";
    public double[] Coordinates { get; set; } = Array.Empty<double>();
}

public class RoutePoint
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public DateTime Timestamp { get; set; }
}

public class AlertInfo
{
    public string Type { get; set; } = string.Empty;
    public decimal? Threshold { get; set; }
    public decimal? Actual { get; set; }
    public string Severity { get; set; } = string.Empty;
}

public class VideoClip
{
    public int Channel { get; set; }
    public DateTime StartTime { get; set; }
    public int Duration { get; set; }
    public string Url { get; set; } = string.Empty;
}
