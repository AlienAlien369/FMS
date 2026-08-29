namespace FMS.Application.Common.DTOs;

public record CreateTenantRequest(
    string Name,
    string Subdomain,
    string CountryCode,
    string? AdminEmail,
    string? AdminFirstName,
    string? AdminLastName,
    string Plan,
    List<string> Sectors
);

public record TenantResponse(
    Guid Id,
    string Name,
    string Subdomain,
    string CountryCode,
    string Timezone,
    string Currency,
    string Plan,
    string Status,
    DateTime CreatedAt
);

public record OnboardingRequest(
    string CompanyName,
    string Subdomain,
    string CountryCode,
    string AdminEmail,
    string AdminPassword,
    string? AdminFirstName,
    string? AdminLastName,
    string Plan,
    List<string> Sectors
);
