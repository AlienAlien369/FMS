namespace FMS.Domain.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? FuelType { get; set; }
    public string? GpsDeviceId { get; set; }
    public string Status { get; set; } = "active";
    public Dictionary<string, object> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
