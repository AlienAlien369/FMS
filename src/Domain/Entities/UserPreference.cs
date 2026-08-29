namespace FMS.Domain.Entities;

public class UserPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Page { get; set; } = string.Empty;
    public string PreferenceType { get; set; } = string.Empty; // table-columns, dashboard-layout, form-config
    public Dictionary<string, object> Config { get; set; } = new();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
