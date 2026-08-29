namespace FMS.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? LicenseNumber { get; set; }
    public DateTime? LicenseExpiry { get; set; }
    public string? Phone { get; set; }
    public decimal BehaviorScore { get; set; }
    public string Status { get; set; } = "active";
    public Dictionary<string, object> Documents { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User? User { get; set; }
}
