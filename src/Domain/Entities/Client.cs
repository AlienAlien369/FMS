namespace FMS.Domain.Entities;

/// <summary>
/// Client/Consignee master data for logistics operations.
/// </summary>
public class Client
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ParentClientId { get; set; }
    public string? CompanyName { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientCode { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PinCode { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CityId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool BillingAddressSame { get; set; } = false;
    public string? BillingAddress { get; set; }
    public string? BillingPinCode { get; set; }
    public Guid? BillingCountryId { get; set; }
    public Guid? BillingStateId { get; set; }
    public Guid? BillingCityId { get; set; }
    public string? CompanyPhone { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactNo { get; set; }
    public string? AltContactNo { get; set; }
    public string? ContactEmail { get; set; }
    public string? MobileNo { get; set; }
    public string? EmailId { get; set; }
    public string? AltEmailId { get; set; }
    public string? PanNo { get; set; }
    public string? GstNo { get; set; }
    public string? CinNo { get; set; }
    public Guid? ConsigneeCategoryId { get; set; }
    public bool IsContractSigned { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Client? ParentClient { get; set; }
}
