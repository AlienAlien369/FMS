namespace FMS.Domain.Entities;

/// <summary>
/// Transport route with start/end locations, waypoints, and map data.
/// </summary>
public class Route
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string StartLocation { get; set; } = string.Empty;
    public string EndLocation { get; set; } = string.Empty;
    public decimal? StartLatitude { get; set; }
    public decimal? StartLongitude { get; set; }
    public decimal? EndLatitude { get; set; }
    public decimal? EndLongitude { get; set; }
    public List<Dictionary<string, object>> Waypoints { get; set; } = new();
    public Guid? RouteTypeId { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? EstimatedDurationMin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
