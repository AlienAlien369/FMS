using System.Security.Claims;
using FMS.Auth.Data;
using FMS.Domain.Entities;
using FMS.SharedKernel.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FMS.Auth.Services;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, string? tenantSubdomain, string? ipAddress);
    Task<RegisterResult> RegisterAsync(RegisterRequest request, string? ipAddress);
    Task<RefreshResult> RefreshTokenAsync(string accessToken, string refreshToken, string? ipAddress);
    Task LogoutAsync(Guid userId, string? refreshToken);
    Task<LoginResult> ExternalLoginAsync(string provider, string providerUserId, string email, string name, string? tenantSubdomain, string? ipAddress);
}

public class AuthService : IAuthService
{
    private readonly AuthDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IBus _bus;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AuthDbContext db, ITokenService tokenService, IBus bus, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _bus = bus;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, string? tenantSubdomain, string? ipAddress)
    {
        // Resolve tenant
        var tenant = tenantSubdomain != null
            ? await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == tenantSubdomain && t.Status == "active")
            : await _db.Tenants.FirstOrDefaultAsync(t => t.Status == "active");

        if (tenant == null)
            return new LoginResult { Success = false, Error = "Tenant not found or inactive" };

        // Find user
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == email && u.TenantId == tenant.Id && u.IsActive);

        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for {Email} on tenant {TenantId}", email, tenant.Id);
            return new LoginResult { Success = false, Error = "Invalid credentials" };
        }

        // Validate password
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Invalid password for {Email} on tenant {TenantId}", email, tenant.Id);
            await LogAudit(user.Id, tenant.Id, "login_failed", "user", user.Id, ipAddress);
            return new LoginResult { Success = false, Error = "Invalid credentials" };
        }

        // Get permissions from role
        var permissions = new List<string>();
        if (user.RoleId.HasValue)
        {
            var role = await _db.Roles.FindAsync(user.RoleId.Value);
            permissions = role?.Permissions ?? new List<string>();
        }

        // Generate tokens
        var accessToken = _tokenService.GenerateAccessToken(user, tenant, permissions);
        var refreshTokenStr = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = _tokenService.HashRefreshToken(refreshTokenStr);

        // Store refresh token
        _db.RefreshTokens.Add(new FMS.Auth.Data.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        });

        // Update last login
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await LogAudit(user.Id, tenant.Id, "login_success", "user", user.Id, ipAddress);

        _logger.LogInformation("User {Email} logged into tenant {TenantName}", email, tenant.Name);

        return new LoginResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Plan = tenant.Plan,
                Permissions = permissions
            }
        };
    }

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, string? ipAddress)
    {
        // Check if tenant subdomain is taken
        if (await _db.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain))
            return new RegisterResult { Success = false, Error = "Subdomain already taken" };

        // Create tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName,
            Subdomain = request.Subdomain,
            CountryCode = request.CountryCode,
            Timezone = request.Timezone ?? "UTC",
            Currency = request.Currency ?? "USD",
            Plan = "trial",
            Status = "active",
            DataResidencyRegion = request.CountryCode == "SA" ? "me-south-1" :
                                  request.CountryCode == "IN" ? "ap-south-1" : "us-east-1"
        };
        _db.Tenants.Add(tenant);

        // Create admin role
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Super Admin",
            Permissions = new List<string> { "*" },
            IsSystemRole = true
        };
        _db.Roles.Add(adminRole);

        // Create admin user
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = request.AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            RoleId = adminRole.Id,
            IsActive = true
        };
        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        // Publish event
        await _bus.Publish(new FMS.MessageBus.Events.TenantOnboardedEvent
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            Subdomain = tenant.Subdomain,
            CountryCode = tenant.CountryCode,
            Plan = tenant.Plan,
            AdminEmail = request.AdminEmail,
            EnabledModules = request.EnabledModules
        });

        _logger.LogInformation("Tenant onboarded: {TenantName} ({Subdomain})", tenant.Name, tenant.Subdomain);

        // Auto-login
        var loginResult = await LoginAsync(request.AdminEmail, request.Password, request.Subdomain, ipAddress);

        return new RegisterResult
        {
            Success = true,
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            LoginResult = loginResult
        };
    }

    public async Task<RefreshResult> RefreshTokenAsync(string accessToken, string refreshToken, string? ipAddress)
    {
        var principal = _tokenService.ValidateToken(accessToken);
        if (principal == null)
            return new RefreshResult { Success = false, Error = "Invalid access token" };

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return new RefreshResult { Success = false, Error = "Invalid token claims" };

        var tokenHash = _tokenService.HashRefreshToken(refreshToken);
        var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(t =>
            t.Token == tokenHash && t.UserId == userId && t.IsActive);

        if (storedToken == null)
        {
            _logger.LogWarning("Refresh token reuse detected for user {UserId}", userId);
            // Token reuse = potential theft → revoke all user tokens
            var allTokens = await _db.RefreshTokens.Where(t => t.UserId == userId && !t.RevokedAt.HasValue).ToListAsync();
            allTokens.ForEach(t => t.RevokedAt = DateTime.UtcNow);
            await _db.SaveChangesAsync();
            return new RefreshResult { Success = false, Error = "Refresh token reuse detected" };
        }

        // Revoke old token (rotation)
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = _tokenService.HashRefreshToken(refreshToken);

        // Generate new tokens
        var user = await _db.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            return new RefreshResult { Success = false, Error = "User not found or inactive" };

        var tenant = await _db.Tenants.FindAsync(user.TenantId);
        if (tenant == null)
            return new RefreshResult { Success = false, Error = "Tenant not found" };

        var permissions = new List<string>();
        if (user.RoleId.HasValue)
        {
            var role = await _db.Roles.FindAsync(user.RoleId.Value);
            permissions = role?.Permissions ?? new List<string>();
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user, tenant, permissions);
        var newRefreshTokenStr = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new FMS.Auth.Data.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = _tokenService.HashRefreshToken(newRefreshTokenStr),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        });

        await _db.SaveChangesAsync();

        return new RefreshResult
        {
            Success = true,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken)
    {
        if (refreshToken != null)
        {
            var tokenHash = _tokenService.HashRefreshToken(refreshToken);
            var storedToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash);
            if (storedToken != null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<LoginResult> ExternalLoginAsync(string provider, string providerUserId, string email, string name, string? tenantSubdomain, string? ipAddress)
    {
        // Find or create user via external provider
        var tenant = tenantSubdomain != null
            ? await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == tenantSubdomain)
            : null;

        if (tenant == null)
            return new LoginResult { Success = false, Error = "Tenant not found" };

        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == email && u.TenantId == tenant.Id);

        if (user == null)
        {
            // Auto-create user from external provider
            var names = name.Split(' ', 2);
            user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                FirstName = names.FirstOrDefault(),
                LastName = names.Length > 1 ? names[1] : null,
                IsActive = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        var permissions = new List<string>();
        if (user.RoleId.HasValue)
        {
            var role = await _db.Roles.FindAsync(user.RoleId.Value);
            permissions = role?.Permissions ?? new List<string>();
        }

        var accessToken = _tokenService.GenerateAccessToken(user, tenant, permissions);
        var refreshTokenStr = _tokenService.GenerateRefreshToken();

        _db.RefreshTokens.Add(new FMS.Auth.Data.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _tokenService.HashRefreshToken(refreshTokenStr),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = ipAddress
        });

        await _db.SaveChangesAsync();

        return new LoginResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Plan = tenant.Plan,
                Permissions = permissions
            }
        };
    }

    private async Task LogAudit(Guid userId, Guid tenantId, string action, string entityType, Guid entityId, string? ipAddress)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}

// ── Request/Response DTOs ──

public class RegisterRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
    public string CountryCode { get; set; } = "US";
    public string? Timezone { get; set; }
    public string? Currency { get; set; }
    public List<string> EnabledModules { get; set; } = new();
}

public class LoginResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserInfo? User { get; set; }
}

public class RegisterResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public LoginResult? LoginResult { get; set; }
}

public class RefreshResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
