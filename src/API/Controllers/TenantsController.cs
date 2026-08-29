using FMS.Application.Common.DTOs;
using FMS.Application.Features.Tenants.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public class TenantsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TenantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("onboard")]
    public async Task<ActionResult<TenantResponse>> OnboardTenant([FromBody] OnboardingRequest request)
    {
        var result = await _mediator.Send(new OnboardTenantCommand(request));
        return CreatedAtAction(nameof(OnboardTenant), new { id = result.Id }, result);
    }

    [HttpGet("check-subdomain/{subdomain}")]
    public async Task<IActionResult> CheckSubdomain(string subdomain)
    {
        // TODO: Check subdomain availability
        return Ok(new { available = true, subdomain });
    }
}
