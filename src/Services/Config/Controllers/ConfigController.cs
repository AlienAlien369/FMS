using FMS.Config.Services;
using FMS.SharedKernel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Config.Controllers;

[ApiController]
[Route("api/v1/config")]
[Authorize]
public class ConfigController : ControllerBase
{
    private readonly IConfigService _configService;

    public ConfigController(IConfigService configService) => _configService = configService;

    [HttpGet("features")]
    public async Task<IActionResult> GetFeatures()
    {
        var features = await _configService.GetFeaturesAsync(GetTenantId());
        return Ok(ApiResponse<List<FeatureDto>>.Ok(features));
    }

    [HttpPut("features/{module}/{featureName}")]
    public async Task<IActionResult> ToggleFeature(string module, string featureName, [FromBody] ToggleFeatureRequest request)
    {
        await _configService.ToggleFeatureAsync(GetTenantId(), module, featureName, request.Enabled);
        return Ok(ApiResponse<object>.Ok(null, $"Feature {module}/{featureName} {(request.Enabled ? "enabled" : "disabled")}"));
    }

    [HttpGet("navigation")]
    public async Task<IActionResult> GetNavigation()
    {
        var permissions = User.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
        var navigation = await _configService.GetNavigationAsync(GetTenantId(), permissions);
        return Ok(ApiResponse<List<NavigationModuleDto>>.Ok(navigation));
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = GetUserId();
        var prefs = await _configService.GetUserPreferencesAsync(userId);
        return Ok(ApiResponse<List<UserPreferenceDto>>.Ok(prefs));
    }

    [HttpPost("preferences")]
    public async Task<IActionResult> SavePreference([FromBody] SavePreferenceRequest request)
    {
        var userId = GetUserId();
        await _configService.SaveUserPreferenceAsync(userId, request.Page, request.PreferenceType, request.Config);
        return Ok(ApiResponse<object>.Ok(null, "Preference saved"));
    }

    [HttpGet("branding")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBranding()
    {
        var branding = await _configService.GetBrandingAsync(GetTenantId());
        return Ok(ApiResponse<BrandingDto>.Ok(branding));
    }

    [HttpPut("branding")]
    public async Task<IActionResult> UpdateBranding([FromBody] BrandingDto branding)
    {
        await _configService.UpdateBrandingAsync(GetTenantId(), branding);
        return Ok(ApiResponse<object>.Ok(null, "Branding updated"));
    }

    private Guid GetTenantId()
    {
        var tenantIdStr = HttpContext.Items["TenantIdString"]?.ToString()
            ?? User.FindFirst("tenant_id")?.Value
            ?? throw new UnauthorizedAccessException("Tenant not resolved");
        return Guid.Parse(tenantIdStr);
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return userIdStr != null ? Guid.Parse(userIdStr) : throw new UnauthorizedAccessException("User not authenticated");
    }
}

public class ToggleFeatureRequest { public bool Enabled { get; set; } }
public class SavePreferenceRequest
{
    public string Page { get; set; } = string.Empty;
    public string PreferenceType { get; set; } = string.Empty;
    public Dictionary<string, object> Config { get; set; } = new();
}
