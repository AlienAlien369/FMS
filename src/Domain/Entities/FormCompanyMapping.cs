namespace FMS.Domain.Entities;

/// <summary>
/// Maps which forms are enabled/disabled per company (tenant).
/// Controls module visibility at the company level.
/// </summary>
public class FormCompanyMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FormId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public FormMaster Form { get; set; } = null!;
}
