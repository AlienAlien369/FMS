using FMS.Application.Common.DTOs;
using FMS.Application.Features.Devices.Commands;
using FMS.Application.Features.Devices.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/fleet/devices")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DevicesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<DeviceResponse>>> GetDevices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var result = await _mediator.Send(new GetDevicesQuery(page, pageSize));
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DeviceResponse>> CreateDevice([FromBody] CreateDeviceRequest request)
    {
        var result = await _mediator.Send(new CreateDeviceCommand(request));
        return CreatedAtAction(nameof(GetDevices), new { id = result.Id }, result);
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] SendDeviceCommandRequest request)
    {
        // TODO: Implement command dispatch via MQTT
        return Ok(new { status = "command_queued", deviceId = request.DeviceId, command = request.CommandType });
    }
}
