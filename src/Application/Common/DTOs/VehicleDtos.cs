namespace FMS.Application.Common.DTOs;

public record CreateVehicleRequest(
    string VehicleNumber,
    string? Type,
    string? Model,
    int? Year,
    string? FuelType,
    string? GpsDeviceId
);

public record UpdateVehicleRequest(
    string? Type,
    string? Model,
    int? Year,
    string? FuelType,
    string? GpsDeviceId,
    string? Status
);

public record VehicleResponse(
    Guid Id,
    string VehicleNumber,
    string? Type,
    string? Model,
    int? Year,
    string? FuelType,
    string Status,
    DateTime CreatedAt
);
