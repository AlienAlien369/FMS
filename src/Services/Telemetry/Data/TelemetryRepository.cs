using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace FMS.Telemetry.Data;

public interface ITelemetryRepository
{
    Task EnsureIndexesAsync();
    Task InsertDeviceRecordAsync(DeviceRecord record);
    Task<List<DeviceRecord>> GetDeviceRecordsAsync(Guid tenantId, Guid? deviceId, DateTime from, DateTime to, int limit = 100);
    Task<DeviceRecord?> GetLatestRecordAsync(Guid tenantId, Guid deviceId);
    Task<List<TripRecord>> GetTripsAsync(Guid tenantId, DateTime? from, DateTime? to);
    Task InsertTripAsync(TripRecord trip);
    Task<List<AlertRecord>> GetAlertsAsync(Guid tenantId, bool? unresolvedOnly, int limit = 50);
    Task InsertAlertAsync(AlertRecord alert);
    Task ResolveAlertAsync(Guid alertId, Guid? resolvedBy, string? notes);
    Task<DashboardStats> GetDashboardStatsAsync(Guid tenantId);
}

public class TelemetryRepository : ITelemetryRepository
{
    private readonly IMongoDatabase _db;
    private readonly ILogger<TelemetryRepository> _logger;

    private IMongoCollection<DeviceRecord> DeviceRecords => _db.GetCollection<DeviceRecord>("device_records");
    private IMongoCollection<TripRecord> Trips => _db.GetCollection<TripRecord>("trips");
    private IMongoCollection<AlertRecord> Alerts => _db.GetCollection<AlertRecord>("alerts");

    public TelemetryRepository(IMongoDatabase db, ILogger<TelemetryRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureIndexesAsync()
    {
        _logger.LogInformation("Creating MongoDB indexes...");

        await DeviceRecords.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<DeviceRecord>(Builders<DeviceRecord>.IndexKeys
                .Ascending(r => r.TenantId).Ascending(r => r.DeviceId).Descending(r => r.Timestamp)),
            new CreateIndexModel<DeviceRecord>(Builders<DeviceRecord>.IndexKeys
                .Ascending(r => r.TenantId))
        });

        await Trips.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<TripRecord>(Builders<TripRecord>.IndexKeys
                .Ascending(t => t.TenantId).Descending(t => t.StartTime)),
            new CreateIndexModel<TripRecord>(Builders<TripRecord>.IndexKeys
                .Ascending(t => t.TenantId).Ascending(t => t.VehicleId))
        });

        await Alerts.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<AlertRecord>(Builders<AlertRecord>.IndexKeys
                .Ascending(a => a.TenantId).Ascending(a => a.Resolved).Descending(a => a.TriggeredAt)),
            new CreateIndexModel<AlertRecord>(Builders<AlertRecord>.IndexKeys
                .Ascending(a => a.TenantId).Ascending(a => a.Severity))
        });

        _logger.LogInformation("MongoDB indexes created successfully");
    }

    public async Task InsertDeviceRecordAsync(DeviceRecord record)
    {
        await DeviceRecords.InsertOneAsync(record);
    }

    public async Task<List<DeviceRecord>> GetDeviceRecordsAsync(Guid tenantId, Guid? deviceId, DateTime from, DateTime to, int limit = 100)
    {
        var filter = Builders<DeviceRecord>.Filter.And(
            Builders<DeviceRecord>.Filter.Eq(r => r.TenantId, tenantId),
            Builders<DeviceRecord>.Filter.Gte(r => r.Timestamp, from),
            Builders<DeviceRecord>.Filter.Lte(r => r.Timestamp, to));

        if (deviceId.HasValue)
            filter = Builders<DeviceRecord>.Filter.And(filter,
                Builders<DeviceRecord>.Filter.Eq(r => r.DeviceId, deviceId.Value));

        return await DeviceRecords.Find(filter)
            .SortByDescending(r => r.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<DeviceRecord?> GetLatestRecordAsync(Guid tenantId, Guid deviceId)
    {
        return await DeviceRecords.Find(r => r.TenantId == tenantId && r.DeviceId == deviceId)
            .SortByDescending(r => r.Timestamp)
            .Limit(1)
            .FirstOrDefaultAsync();
    }

    public async Task<List<TripRecord>> GetTripsAsync(Guid tenantId, DateTime? from, DateTime? to)
    {
        var filter = Builders<TripRecord>.Filter.Eq(t => t.TenantId, tenantId);

        if (from.HasValue)
            filter = Builders<TripRecord>.Filter.And(filter,
                Builders<TripRecord>.Filter.Gte(t => t.StartTime, from.Value));
        if (to.HasValue)
            filter = Builders<TripRecord>.Filter.And(filter,
                Builders<TripRecord>.Filter.Lte(t => t.StartTime, to.Value));

        return await Trips.Find(filter).SortByDescending(t => t.StartTime).ToListAsync();
    }

    public async Task InsertTripAsync(TripRecord trip) => await Trips.InsertOneAsync(trip);

    public async Task<List<AlertRecord>> GetAlertsAsync(Guid tenantId, bool? unresolvedOnly, int limit = 50)
    {
        var filter = Builders<AlertRecord>.Filter.Eq(a => a.TenantId, tenantId);
        if (unresolvedOnly == true)
            filter = Builders<AlertRecord>.Filter.And(filter,
                Builders<AlertRecord>.Filter.Eq(a => a.Resolved, false));

        return await Alerts.Find(filter)
            .SortByDescending(a => a.TriggeredAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task InsertAlertAsync(AlertRecord alert) => await Alerts.InsertOneAsync(alert);

    public async Task ResolveAlertAsync(Guid alertId, Guid? resolvedBy, string? notes)
    {
        var update = Builders<AlertRecord>.Update
            .Set(a => a.Resolved, true)
            .Set(a => a.ResolvedAt, DateTime.UtcNow)
            .Set(a => a.ResolvedByUserId, resolvedBy)
            .Set(a => a.ResolutionNotes, notes);

        await Alerts.UpdateOneAsync(a => a.AlertId == alertId, update);
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(Guid tenantId)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("TenantId", tenantId.ToString())),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$DeviceId" },
                { "latest_timestamp", new BsonDocument("$max", "$Timestamp") },
                { "record_count", new BsonDocument("$sum", 1) }
            })
        };

        var results = await DeviceRecords.Aggregate<BsonDocument>(pipeline).ToListAsync();

        var onlineDevices = results.Count(r =>
            r["latest_timestamp"].ToUniversalTime() > DateTime.UtcNow.AddMinutes(-5));

        var unresolvedAlerts = await Alerts.CountDocumentsAsync(
            Builders<AlertRecord>.Filter.And(
                Builders<AlertRecord>.Filter.Eq(a => a.TenantId, tenantId),
                Builders<AlertRecord>.Filter.Eq(a => a.Resolved, false)));

        var activeTrips = await Trips.CountDocumentsAsync(
            Builders<TripRecord>.Filter.And(
                Builders<TripRecord>.Filter.Eq(t => t.TenantId, tenantId),
                Builders<TripRecord>.Filter.Eq(t => t.Status, "in_progress")));

        return new DashboardStats
        {
            TotalDevices = results.Count,
            OnlineDevices = onlineDevices,
            OfflineDevices = results.Count - onlineDevices,
            UnresolvedAlerts = (int)unresolvedAlerts,
            ActiveTrips = (int)activeTrips
        };
    }
}

// ── MongoDB Models ──

public class DeviceRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public Guid TenantId { get; set; }
    public Guid DeviceId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    /// <summary>GeoJSON location stored as embedded document.</summary>
    public BsonDocument? Location { get; set; }

    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public bool? Ignition { get; set; }
    public double? Odometer { get; set; }
    public double? FuelLevel { get; set; }
    public double? Temperature { get; set; }
    public int? SignalStrength { get; set; }
    public int? BatteryLevel { get; set; }
    public BsonDocument? Alerts { get; set; }
    public BsonDocument? RawPayload { get; set; }

    /// <summary>Helper to read Location as a GeoJsonPoint.</summary>
    public GeoJsonPoint? GetLocation() => Location != null ? new GeoJsonPoint
    {
        Type = Location.Contains("type") ? Location["type"].AsString : "Point",
        Coordinates = Location.Contains("coordinates")
            ? Location["coordinates"].AsBsonArray.Select(c => c.ToDouble()).ToArray()
            : Array.Empty<double>()
    } : null;
}

public class GeoJsonPoint
{
    public string Type { get; set; } = "Point";
    public double[] Coordinates { get; set; } = Array.Empty<double>();
}

public class TripRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public Guid TripId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "planned";
    public double? TotalDistanceKm { get; set; }
    public double? TotalFuelConsumed { get; set; }
    public int DeviationCount { get; set; }
}

public class AlertRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public Guid AlertId { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? DeviceId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public BsonDocument? Location { get; set; }
    public double? Speed { get; set; }
    public double? Threshold { get; set; }
    public string? Message { get; set; }
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool Resolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class DashboardStats
{
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int OfflineDevices { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int ActiveTrips { get; set; }
}
