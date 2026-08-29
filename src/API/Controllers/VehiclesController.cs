using FMS.Application.Common.DTOs;
using FMS.Application.Features.Vehicles.Commands;
using FMS.Application.Features.Vehicles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/fleet/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly IMediator _mediator;

    public VehiclesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<VehicleResponse>>> GetVehicles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        var result = await _mediator.Send(new GetVehiclesQuery(page, pageSize, sortBy, sortOrder));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        var result = await _mediator.Send(new CreateVehicleCommand(request));
        return CreatedAtAction(nameof(GetVehicles), new { id = result.Id }, result);
    }
}
