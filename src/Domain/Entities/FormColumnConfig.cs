namespace FMS.Domain.Entities;

/// <summary>
/// Per-form column visibility configuration per company.
/// Controls which columns appear in tables/grids.
/// </summary>
public class FormColumnConfig
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FormId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public FormMaster Form { get; set; } = null!;
}
