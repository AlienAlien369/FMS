namespace FMS.Domain.Entities;

public class Device
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid VendorId { get; set; }
    public string? Imei { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? FirmwareVersion { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public string Status { get; set; } = "active";
    public Dictionary<string, object> Config { get; set; } = new();
    public DateTime? LastSeen { get; set; }
    public decimal? LastSpeed { get; set; }
    public int? SignalStrength { get; set; }
    public int? BatteryLevel { get; set; }
    public DateTime? InstalledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public DeviceVendor Vendor { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
    public Driver? Driver { get; set; }
}
