using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Drivers.Queries;

public record GetDriversQuery(int Page = 1, int PageSize = 25)
    : IRequest<PagedResponse<DriverResponse>>;

public class GetDriversHandler : IRequestHandler<GetDriversQuery, PagedResponse<DriverResponse>>
{
    private readonly IGenericRepository<Driver> _repository;
    private readonly ICurrentUserService _currentUser;

    public GetDriversHandler(IGenericRepository<Driver> repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<PagedResponse<DriverResponse>> Handle(GetDriversQuery request, CancellationToken cancellationToken)
    {
        var allDrivers = await _repository.GetAllAsync();
        var tenantDrivers = allDrivers.Where(d => d.TenantId == _currentUser.TenantId).ToList();
        var totalCount = tenantDrivers.Count;

        var pagedDrivers = tenantDrivers
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DriverResponse(
                d.Id,
                d.FirstName,
                d.LastName,
                d.LicenseNumber,
                d.LicenseExpiry,
                d.Phone,
                d.BehaviorScore,
                d.Status,
                d.CreatedAt
            ))
            .ToList();

        return new PagedResponse<DriverResponse>(pagedDrivers, totalCount, request.Page, request.PageSize);
    }
}
