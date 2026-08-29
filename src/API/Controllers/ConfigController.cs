using FMS.Application.Common.DTOs;
using FMS.Application.Features.Config.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.API.Controllers;

[ApiController]
[Route("api/v1/config")]
[Authorize]
public class ConfigController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("navigation")]
    public async Task<ActionResult<NavigationResponse>> GetNavigation()
    {
        var result = await _mediator.Send(new GetNavigationQuery());
        return Ok(result);
    }

    [HttpGet("branding")]
    public ActionResult<BrandingResponse> GetBranding()
    {
        // TODO: Fetch from tenant settings
        return Ok(new BrandingResponse(
            "#1e40af",
            "#3b82f6",
            "/assets/logo.svg",
            "/assets/favicon.ico",
            "Inter",
            "FMS Fleet Management"
        ));
    }
}
