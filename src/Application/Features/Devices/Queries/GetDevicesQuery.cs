using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Devices.Queries;

public record GetDevicesQuery(int Page = 1, int PageSize = 25)
    : IRequest<PagedResponse<DeviceResponse>>;

public class GetDevicesHandler : IRequestHandler<GetDevicesQuery, PagedResponse<DeviceResponse>>
{
    private readonly IDeviceRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetDevicesHandler(IDeviceRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<DeviceResponse>> Handle(GetDevicesQuery request, CancellationToken cancellationToken)
    {
        var devices = await _repository.GetByTenantIdAsync(_currentUser.TenantId);
        var totalCount = devices.Count;

        var pagedDevices = devices
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DeviceResponse(
                d.Id,
                d.Imei,
                d.SerialNumber,
                d.Model,
                d.FirmwareVersion,
                d.VehicleId,
                d.Status,
                d.LastSeen,
                d.SignalStrength,
                d.BatteryLevel,
                d.CreatedAt
            ))
            .ToList();

        return new PagedResponse<DeviceResponse>(pagedDevices, totalCount, request.Page, request.PageSize);
    }
}
