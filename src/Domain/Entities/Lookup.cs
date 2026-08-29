namespace FMS.Domain.Entities;

/// <summary>
/// Dynamic lookup values for all dropdowns across the system.
/// Supports cascading via ParentId (e.g., State→Country, City→State).
/// Categories: Country, State, City, VehicleType, FuelType, DeviceProtocol,
/// RouteType, LocationType, GeofenceColor, SubscriptionPackage, PaymentMode,
/// IncidentSeverity, IncidentStatus, DeliveryStatus, CompanyType, ConsigneeCategory, UserFor
/// </summary>
public class Lookup
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty; // 'Country', 'VehicleType', etc.
    public Guid? ParentId { get; set; } // For cascading (State→Country)
    public string Code { get; set; } = string.Empty; // 'IN', 'US', 'SA'
    public string Label { get; set; } = string.Empty; // 'India', 'United States'
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public Dictionary<string, object> Metadata { get; set; } = new(); // phone code, currency, etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Lookup? Parent { get; set; }
    public ICollection<Lookup> Children { get; set; } = new List<Lookup>();
}
