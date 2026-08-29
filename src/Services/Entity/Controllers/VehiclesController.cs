using FMS.Entity.Features.Vehicles;
using FMS.SharedKernel.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Entity.Controllers;

[ApiController]
[Route("api/v1/fleet/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VehiclesController> _logger;

    public VehiclesController(IMediator mediator, ILogger<VehiclesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get paginated vehicles for the current tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VehicleDto>>), 200)]
    public async Task<IActionResult> GetVehicles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        var result = await _mediator.Send(new GetVehiclesQuery(page, pageSize, search, sortBy, sortOrder));

        if (!result.IsSuccess)
            return NotFound(ApiResponse<PagedResult<VehicleDto>>.Fail(result.Error!));

        return Ok(ApiResponse<PagedResult<VehicleDto>>.Ok(result.Value!));
    }

    /// <summary>Get a single vehicle by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
    public async Task<IActionResult> GetVehicle(Guid id)
    {
        var result = await _mediator.Send(new GetVehicleByIdQuery(id));

        if (!result.IsSuccess)
            return NotFound(ApiResponse<VehicleDto>.Fail(result.Error!));

        return Ok(ApiResponse<VehicleDto>.Ok(result.Value!));
    }

    /// <summary>Create a new vehicle.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 201)]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        var result = await _mediator.Send(new CreateVehicleCmd(
            request.VehicleNumber, request.Type, request.Model,
            request.Year, request.FuelType, request.GpsDeviceId));

        if (!result.IsSuccess)
            return BadRequest(ApiResponse<VehicleDto>.Fail(result.Error!));

        _logger.LogInformation("Vehicle created: {VehicleNumber}", request.VehicleNumber);
        return CreatedAtAction(nameof(GetVehicle), new { id = result.Value!.Id },
            ApiResponse<VehicleDto>.Ok(result.Value));
    }

    /// <summary>Update an existing vehicle.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VehicleDto>), 200)]
    public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] UpdateVehicleRequest request)
    {
        var result = await _mediator.Send(new UpdateVehicleCmd(
            id, request.VehicleNumber, request.Type, request.Model,
            request.Year, request.FuelType, request.Status));

        if (!result.IsSuccess)
            return NotFound(ApiResponse<VehicleDto>.Fail(result.Error!));

        return Ok(ApiResponse<VehicleDto>.Ok(result.Value!));
    }

    /// <summary>Soft-delete a vehicle.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        var result = await _mediator.Send(new DeleteVehicleCmd(id));

        if (!result.IsSuccess)
            return NotFound(ApiResponse<object>.Fail(result.Error!));

        return NoContent();
    }
}

// ── Request DTOs ──

public class CreateVehicleRequest
{
    public string VehicleNumber { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? FuelType { get; set; }
    public string? GpsDeviceId { get; set; }
}

public class UpdateVehicleRequest
{
    public string? VehicleNumber { get; set; }
    public string? Type { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? FuelType { get; set; }
    public string? Status { get; set; }
}
