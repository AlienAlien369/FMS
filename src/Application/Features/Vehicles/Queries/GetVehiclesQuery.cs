using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Vehicles.Queries;

public record GetVehiclesQuery(int Page = 1, int PageSize = 25, string? SortBy = null, string? SortOrder = null) 
    : IRequest<PagedResponse<VehicleResponse>>;

public class GetVehiclesHandler : IRequestHandler<GetVehiclesQuery, PagedResponse<VehicleResponse>>
{
    private readonly IVehicleRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetVehiclesHandler(IVehicleRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<VehicleResponse>> Handle(GetVehiclesQuery request, CancellationToken cancellationToken)
    {
        var vehicles = await _repository.GetByTenantIdAsync(_currentUser.TenantId);
        var totalCount = vehicles.Count;

        var pagedVehicles = vehicles
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(v => new VehicleResponse(
                v.Id,
                v.VehicleNumber,
                v.Type,
                v.Model,
                v.Year,
                v.FuelType,
                v.Status,
                v.CreatedAt
            ))
            .ToList();

        return new PagedResponse<VehicleResponse>(pagedVehicles, totalCount, request.Page, request.PageSize);
    }
}
