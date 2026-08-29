namespace FMS.Domain.Entities;

public class DeviceVendor
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty; // mqtt, tcp, udp, http
    public int? DefaultPort { get; set; }
    public bool SupportsVideo { get; set; }
    public bool SupportsFuel { get; set; }
    public bool SupportsTemperature { get; set; }
    public bool SupportsCanBus { get; set; }
    public Dictionary<string, object> SchemaConfig { get; set; } = new();
    public string AdapterVersion { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
