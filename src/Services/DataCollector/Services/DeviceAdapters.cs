using FMS.MessageBus.Events;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FMS.DataCollector.Services;

/// <summary>
/// Registry of vendor-specific device adapters.
/// Each adapter knows how to parse a vendor's payload format into standard telemetry.
/// </summary>
public interface IDeviceAdapterRegistry
{
    IDeviceAdapter GetAdapter(string vendorCode);
}

public class DeviceAdapterRegistry : IDeviceAdapterRegistry
{
    private readonly Dictionary<string, IDeviceAdapter> _adapters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<DeviceAdapterRegistry> _logger;

    public DeviceAdapterRegistry(ILogger<DeviceAdapterRegistry> logger)
    {
        _logger = logger;

        // Register built-in adapters
        _adapters["itriangle"] = new ITriangleAdapter(logger);
        _adapters["streamax"] = new StreamaxAdapter(logger);
        _adapters["teltonika"] = new TeltonikaAdapter(logger);
        _adapters["generic"] = new GenericJsonAdapter(logger);

        _logger.LogInformation("Registered {Count} device adapters: {Adapters}",
            _adapters.Count, string.Join(", ", _adapters.Keys));
    }

    public IDeviceAdapter GetAdapter(string vendorCode)
    {
        if (_adapters.TryGetValue(vendorCode, out var adapter))
            return adapter;

        _logger.LogWarning("Unknown vendor '{VendorCode}', using generic adapter", vendorCode);
        return _adapters["generic"];
    }
}

public interface IDeviceAdapter
{
    string VendorCode { get; }
    StandardTelemetry ParsePayload(string rawPayload, Guid tenantId, Guid deviceId, string vendorCode);
}

/// <summary>
/// Standard telemetry model that all adapters produce.
/// </summary>
public class StandardTelemetry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public bool? Ignition { get; set; }
    public double? Odometer { get; set; }
    public double? FuelLevel { get; set; }
    public double? Temperature { get; set; }
    public Dictionary<string, object>? RawPayload { get; set; }
}

/// <summary>
/// iTriangle VT300 adapter (TCP binary / JSON hybrid).
/// </summary>
public class ITriangleAdapter : IDeviceAdapter
{
    private readonly ILogger _logger;
    public string VendorCode => "itriangle";

    public ITriangleAdapter(ILogger logger) => _logger = logger;

    public StandardTelemetry ParsePayload(string rawPayload, Guid tenantId, Guid deviceId, string vendorCode)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(rawPayload);
            return new StandardTelemetry
            {
                Timestamp = json.TryGetProperty("timestamp", out var ts) ? ts.GetDateTime() : DateTime.UtcNow,
                Latitude = json.GetProperty("gps").GetProperty("lat").GetDouble(),
                Longitude = json.GetProperty("gps").GetProperty("lng").GetDouble(),
                Speed = json.TryGetProperty("speed", out var s) ? s.GetDouble() : null,
                Heading = json.TryGetProperty("direction", out var d) ? d.GetDouble() : null,
                Ignition = json.TryGetProperty("io", out var io) && io.TryGetProperty("ignition", out var ig) ? ig.GetBoolean() : null,
                Odometer = json.TryGetProperty("mileage", out var o) ? o.GetDouble() : null,
                FuelLevel = json.TryGetProperty("fuel", out var fuelObj) && fuelObj.TryGetProperty("level", out var f) ? f.GetDouble() : null,
                RawPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(rawPayload)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse iTriangle payload for device {DeviceId}", deviceId);
            return new StandardTelemetry { RawPayload = new Dictionary<string, object> { ["raw"] = rawPayload } };
        }
    }
}

/// <summary>
/// Streamax video telematics adapter (MQTT JSON).
/// </summary>
public class StreamaxAdapter : IDeviceAdapter
{
    private readonly ILogger _logger;
    public string VendorCode => "streamax";

    public StreamaxAdapter(ILogger logger) => _logger = logger;

    public StandardTelemetry ParsePayload(string rawPayload, Guid tenantId, Guid deviceId, string vendorCode)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(rawPayload);
            return new StandardTelemetry
            {
                Timestamp = json.TryGetProperty("time", out var ts) ? ts.GetDateTime() : DateTime.UtcNow,
                Latitude = json.GetProperty("gps").GetProperty("latitude").GetDouble(),
                Longitude = json.GetProperty("gps").GetProperty("longitude").GetDouble(),
                Speed = json.TryGetProperty("vehicle", out var v) && v.TryGetProperty("speed", out var s) ? s.GetDouble() : null,
                Heading = json.TryGetProperty("heading", out var h) ? h.GetDouble() : null,
                Ignition = json.TryGetProperty("ignition", out var ig) ? ig.GetBoolean() : null,
                RawPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(rawPayload)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Streamax payload for device {DeviceId}", deviceId);
            return new StandardTelemetry { RawPayload = new Dictionary<string, object> { ["raw"] = rawPayload } };
        }
    }
}

/// <summary>
/// Teltonika FMC130/600 adapter (TCP binary, first parses to JSON).
/// </summary>
public class TeltonikaAdapter : IDeviceAdapter
{
    private readonly ILogger _logger;
    public string VendorCode => "teltonika";

    public TeltonikaAdapter(ILogger logger) => _logger = logger;

    public StandardTelemetry ParsePayload(string rawPayload, Guid tenantId, Guid deviceId, string vendorCode)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(rawPayload);
            return new StandardTelemetry
            {
                Timestamp = json.TryGetProperty("timestamp", out var ts) ? ts.GetDateTime() : DateTime.UtcNow,
                Latitude = json.GetProperty("latitude").GetDouble(),
                Longitude = json.GetProperty("longitude").GetDouble(),
                Speed = json.TryGetProperty("speed", out var s) ? s.GetDouble() / 10.0 : null, // Teltonika uses 0.1 km/h
                Heading = json.TryGetProperty("angle", out var a) ? a.GetDouble() : null,
                Ignition = json.TryGetProperty("io", out var ioTel) && ioTel.TryGetProperty("1", out var ig) ? ig.GetBoolean() : null,
                Odometer = json.TryGetProperty("odometer", out var o) ? o.GetDouble() : null,
                FuelLevel = json.TryGetProperty("io", out var ioFuel) && ioFuel.TryGetProperty("81", out var f) ? f.GetDouble() : null,
                Temperature = json.TryGetProperty("io", out var ioTemp) && ioTemp.TryGetProperty("68", out var t) ? t.GetDouble() / 10.0 : null,
                RawPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(rawPayload)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Teltonika payload for device {DeviceId}", deviceId);
            return new StandardTelemetry { RawPayload = new Dictionary<string, object> { ["raw"] = rawPayload } };
        }
    }
}

/// <summary>
/// Generic JSON adapter for unknown vendors.
/// </summary>
public class GenericJsonAdapter : IDeviceAdapter
{
    private readonly ILogger _logger;
    public string VendorCode => "generic";

    public GenericJsonAdapter(ILogger logger) => _logger = logger;

    public StandardTelemetry ParsePayload(string rawPayload, Guid tenantId, Guid deviceId, string vendorCode)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(rawPayload);
            return new StandardTelemetry
            {
                Latitude = json.TryGetProperty("lat", out var lat) ? lat.GetDouble() :
                           json.TryGetProperty("latitude", out var lat2) ? lat2.GetDouble() : 0,
                Longitude = json.TryGetProperty("lng", out var lng) ? lng.GetDouble() :
                            json.TryGetProperty("longitude", out var lng2) ? lng2.GetDouble() : 0,
                Speed = json.TryGetProperty("speed", out var s) ? s.GetDouble() : null,
                RawPayload = JsonSerializer.Deserialize<Dictionary<string, object>>(rawPayload)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse generic payload for device {DeviceId}", deviceId);
            return new StandardTelemetry { RawPayload = new Dictionary<string, object> { ["raw"] = rawPayload } };
        }
    }
}
