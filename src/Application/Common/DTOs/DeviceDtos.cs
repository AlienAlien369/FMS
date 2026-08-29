namespace FMS.Application.Common.DTOs;

public record CreateDeviceRequest(
    Guid VendorId,
    string? Imei,
    string? SerialNumber,
    string? Model,
    Guid? VehicleId,
    Guid? DriverId
);

public record DeviceResponse(
    Guid Id,
    string? Imei,
    string? SerialNumber,
    string? Model,
    string? FirmwareVersion,
    Guid? VehicleId,
    string Status,
    DateTime? LastSeen,
    int? SignalStrength,
    int? BatteryLevel,
    DateTime CreatedAt
);

public record SendDeviceCommandRequest(
    Guid DeviceId,
    string CommandType,
    Dictionary<string, object>? Payload
);
