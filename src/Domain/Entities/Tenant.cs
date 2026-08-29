namespace FMS.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public string CountryCode { get; set; } = "US";
    public string Timezone { get; set; } = "UTC";
    public string Currency { get; set; } = "USD";
    public string Plan { get; set; } = "basic"; // basic, pro, enterprise
    public string Status { get; set; } = "trial"; // active, suspended, trial
    public string DataResidencyRegion { get; set; } = "us-east-1";
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<Feature> Features { get; set; } = new List<Feature>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
