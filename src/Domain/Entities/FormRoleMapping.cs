namespace FMS.Domain.Entities;

/// <summary>
/// Maps roles to form access rights (View/Add/Edit/Delete).
/// Determines what each role can do on each form/page.
/// </summary>
public class FormRoleMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Guid FormId { get; set; }
    public bool CanView { get; set; } = false;
    public bool CanAdd { get; set; } = false;
    public bool CanEdit { get; set; } = false;
    public bool CanDelete { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public FormMaster Form { get; set; } = null!;
}
