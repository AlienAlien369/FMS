namespace FMS.Application.Common.DTOs;

public record LoginRequest(
    string Email,
    string Password,
    string? TenantSubdomain
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserResponse User
);

public record UserResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? RoleName,
    List<string> Permissions,
    string TenantId,
    string TenantName
);

public record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName
);
