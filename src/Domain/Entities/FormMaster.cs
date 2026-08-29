namespace FMS.Domain.Entities;

/// <summary>
/// System form/page registry for RBAC configuration.
/// Maps form names to controllers for permission management.
/// </summary>
public class FormMaster
{
    public Guid Id { get; set; }
    public string FormName { get; set; } = string.Empty;
    public string ControllerName { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public Guid? ParentFormId { get; set; }
    public string? AreaName { get; set; }
    public string Platform { get; set; } = "Web"; // Web, Mobile, Both
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public FormMaster? ParentForm { get; set; }
}
