using FMS.Auth.Services;
using FMS.SharedKernel.Models;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Auth.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResult>), 200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(
            request.Email, request.Password, request.TenantSubdomain, GetClientIp());

        if (!result.Success)
            return Unauthorized(ApiResponse<LoginResult>.Fail(result.Error!));

        return Ok(ApiResponse<LoginResult>.Ok(result));
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResult>), 201)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request, GetClientIp());

        if (!result.Success)
            return BadRequest(ApiResponse<RegisterResult>.Fail(result.Error!));

        return StatusCode(201, ApiResponse<RegisterResult>.Ok(result));
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshResult>), 200)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshTokenAsync(
            request.AccessToken, request.RefreshToken, GetClientIp());

        if (!result.Success)
            return Unauthorized(ApiResponse<RefreshResult>.Fail(result.Error!));

        return Ok(ApiResponse<RefreshResult>.Ok(result));
    }

    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        await _authService.LogoutAsync(userId, request?.RefreshToken);
        return Ok(ApiResponse<object>.Ok(null, "Logged out successfully"));
    }

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}

// ── Request DTOs ──

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? TenantSubdomain { get; set; }
}

public class RefreshRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}
