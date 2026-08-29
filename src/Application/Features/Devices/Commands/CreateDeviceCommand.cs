using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Devices.Commands;

public record CreateDeviceCommand(CreateDeviceRequest Request) : IRequest<DeviceResponse>;

public class CreateDeviceHandler : IRequestHandler<CreateDeviceCommand, DeviceResponse>
{
    private readonly IDeviceRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CreateDeviceHandler(IDeviceRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<DeviceResponse> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        var device = new Device
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VendorId = request.Request.VendorId,
            Imei = request.Request.Imei,
            SerialNumber = request.Request.SerialNumber,
            Model = request.Request.Model,
            VehicleId = request.Request.VehicleId,
            DriverId = request.Request.DriverId,
            Status = "active"
        };

        await _repository.AddAsync(device);

        return new DeviceResponse(
            device.Id,
            device.Imei,
            device.SerialNumber,
            device.Model,
            device.FirmwareVersion,
            device.VehicleId,
            device.Status,
            device.LastSeen,
            device.SignalStrength,
            device.BatteryLevel,
            device.CreatedAt
        );
    }
}
