using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Vehicles.Commands;

public record CreateVehicleCommand(CreateVehicleRequest Request) : IRequest<VehicleResponse>;

public class CreateVehicleHandler : IRequestHandler<CreateVehicleCommand, VehicleResponse>
{
    private readonly IVehicleRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CreateVehicleHandler(IVehicleRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<VehicleResponse> Handle(CreateVehicleCommand request, CancellationToken cancellationToken)
    {
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            VehicleNumber = request.Request.VehicleNumber,
            Type = request.Request.Type,
            Model = request.Request.Model,
            Year = request.Request.Year,
            FuelType = request.Request.FuelType,
            GpsDeviceId = request.Request.GpsDeviceId,
            Status = "active"
        };

        await _repository.AddAsync(vehicle);

        return new VehicleResponse(
            vehicle.Id,
            vehicle.VehicleNumber,
            vehicle.Type,
            vehicle.Model,
            vehicle.Year,
            vehicle.FuelType,
            vehicle.Status,
            vehicle.CreatedAt
        );
    }
}
