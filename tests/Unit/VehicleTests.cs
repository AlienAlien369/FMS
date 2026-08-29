using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Application.Features.Vehicles.Commands;
using FMS.Application.Features.Vehicles.Queries;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace FMS.Tests.Unit;

public class VehicleTests
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly CreateVehicleHandler _createHandler;
    private readonly GetVehiclesHandler _getHandler;

    public VehicleTests()
    {
        _vehicleRepository = Substitute.For<IVehicleRepository>();
        _currentUser = Substitute.For<ICurrentUserService>();
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _createHandler = new CreateVehicleHandler(_vehicleRepository, _currentUser);
        _getHandler = new GetVehiclesHandler(_vehicleRepository, _currentUser);
    }

    [Fact]
    public async Task CreateVehicle_ShouldReturnVehicleResponse()
    {
        // Arrange
        var request = new CreateVehicleRequest("MH-01-AB-1234", "Truck", "Tata Prima", 2024, "Diesel", null);

        _vehicleRepository.AddAsync(Arg.Any<Vehicle>())
            .Returns(ci => ci.Arg<Vehicle>());

        // Act
        var result = await _createHandler.Handle(new CreateVehicleCommand(request), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.VehicleNumber.Should().Be("MH-01-AB-1234");
        result.Type.Should().Be("Truck");
        result.Status.Should().Be("active");
    }

    [Fact]
    public async Task GetVehicles_ShouldReturnPagedResponse()
    {
        // Arrange
        var vehicles = new List<Vehicle>
        {
            new() { Id = Guid.NewGuid(), TenantId = _currentUser.TenantId, VehicleNumber = "MH-01-AB-1234", Status = "active" },
            new() { Id = Guid.NewGuid(), TenantId = _currentUser.TenantId, VehicleNumber = "MH-02-CD-5678", Status = "active" }
        };

        _vehicleRepository.GetByTenantIdAsync(_currentUser.TenantId)
            .Returns(vehicles);

        // Act
        var result = await _getHandler.Handle(new GetVehiclesQuery(1, 25), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }
}
