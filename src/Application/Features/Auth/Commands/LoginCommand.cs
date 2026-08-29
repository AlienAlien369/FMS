using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Auth.Commands;

public record LoginCommand(LoginRequest Request) : IRequest<LoginResponse>;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly IGenericRepository<Role> _roleRepository;

    public LoginHandler(
        ITenantRepository tenantRepository,
        IGenericRepository<User> userRepository,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUser,
        IGenericRepository<Role> roleRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
        _roleRepository = roleRepository;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Request.Email;
        var password = request.Request.Password;
        var subdomain = request.Request.TenantSubdomain;

        // Resolve tenant: by subdomain if provided, otherwise search all tenants
        Domain.Entities.Tenant? tenant = null;
        User? user = null;

        if (!string.IsNullOrEmpty(subdomain))
        {
            tenant = await _tenantRepository.GetBySubdomainAsync(subdomain);
            if (tenant == null)
                throw new InvalidOperationException("Tenant not found");

            var users = await _userRepository.FindAsync(u =>
                u.Email == email && u.TenantId == tenant.Id);
            user = users.FirstOrDefault();
        }
        else
        {
            // Find user by email across all tenants
            var allUsers = await _userRepository.FindAsync(u => u.Email == email);
            user = allUsers.FirstOrDefault();
            if (user != null)
            {
                tenant = await _tenantRepository.GetByIdAsync(user.TenantId);
            }
        }

        if (user == null || tenant == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        // Validate password using BCrypt
        bool passwordValid = false;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch
        {
            // If BCrypt verification fails (e.g. legacy plain-text hash), fallback
            passwordValid = user.PasswordHash == password;
        }

        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid email or password");

        // Get role and permissions
        var permissions = new List<string>();
        string? roleName = null;
        if (user.RoleId.HasValue)
        {
            var roles = await _roleRepository.FindAsync(r => r.Id == user.RoleId.Value);
            var role = roles.FirstOrDefault();
            if (role != null)
            {
                permissions = role.Permissions ?? new List<string>();
                roleName = role.Name;
            }
        }

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user, tenant, permissions);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        return new LoginResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(15),
            new UserResponse(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                roleName,
                permissions,
                tenant.Id.ToString(),
                tenant.Name
            )
        );
    }
}
