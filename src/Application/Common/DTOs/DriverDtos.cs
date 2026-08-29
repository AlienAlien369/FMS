namespace FMS.Application.Common.DTOs;

public record CreateDriverRequest(
    string? FirstName,
    string? LastName,
    string? LicenseNumber,
    DateTime? LicenseExpiry,
    string? Phone
);

public record DriverResponse(
    Guid Id,
    string? FirstName,
    string? LastName,
    string? LicenseNumber,
    DateTime? LicenseExpiry,
    string? Phone,
    decimal BehaviorScore,
    string Status,
    DateTime CreatedAt
);
