using FMS.Application.Common.DTOs;
using FMS.Application.Features.Drivers.Commands;
using FMS.Application.Features.Drivers.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/fleet/drivers")]
[Authorize]
public class DriversController : ControllerBase
{
    private readonly IMediator _mediator;

    public DriversController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<DriverResponse>>> GetDrivers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await _mediator.Send(new GetDriversQuery(page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DriverResponse>> CreateDriver([FromBody] CreateDriverRequest request)
    {
        var result = await _mediator.Send(new CreateDriverCommand(request));
        return CreatedAtAction(nameof(GetDrivers), new { id = result.Id }, result);
    }
}
