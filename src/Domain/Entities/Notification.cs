namespace FMS.Domain.Entities;

/// <summary>
/// In-app notification for users.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, error, success
    public bool IsRead { get; set; } = false;
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
}
