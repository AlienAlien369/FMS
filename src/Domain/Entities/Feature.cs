namespace FMS.Domain.Entities;

public class Feature
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public Dictionary<string, object> Config { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
