using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Drivers.Commands;

public record CreateDriverCommand(CreateDriverRequest Request) : IRequest<DriverResponse>;

public class CreateDriverHandler : IRequestHandler<CreateDriverCommand, DriverResponse>
{
    private readonly IGenericRepository<Driver> _repository;
    private readonly ICurrentUserService _currentUser;

    public CreateDriverHandler(IGenericRepository<Driver> repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<DriverResponse> Handle(CreateDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = new Driver
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            FirstName = request.Request.FirstName,
            LastName = request.Request.LastName,
            LicenseNumber = request.Request.LicenseNumber,
            LicenseExpiry = request.Request.LicenseExpiry,
            Phone = request.Request.Phone,
            Status = "active"
        };

        await _repository.AddAsync(driver);

        return new DriverResponse(
            driver.Id,
            driver.FirstName,
            driver.LastName,
            driver.LicenseNumber,
            driver.LicenseExpiry,
            driver.Phone,
            driver.BehaviorScore,
            driver.Status,
            driver.CreatedAt
        );
    }
}
