using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Auth.Commands;

public record LoginCommand(LoginRequest Request) : IRequest<LoginResponse>;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUser;

    public LoginHandler(
        ITenantRepository tenantRepository,
        IGenericRepository<Domain.Entities.User> userRepository,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUser)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Resolve tenant
        var tenant = request.Request.TenantSubdomain != null
            ? await _tenantRepository.GetBySubdomainAsync(request.Request.TenantSubdomain)
            : null;

        if (tenant == null)
            throw new InvalidOperationException("Tenant not found");

        // Find user
        var users = await _userRepository.FindAsync(u => 
            u.Email == request.Request.Email && u.TenantId == tenant.Id);
        
        var user = users.FirstOrDefault();
        if (user == null)
            throw new UnauthorizedAccessException("Invalid credentials");

        // Validate password (simplified - in production use BCrypt)
        if (user.PasswordHash != request.Request.Password)
            throw new UnauthorizedAccessException("Invalid credentials");

        // Get permissions
        var permissions = new List<string>(); // TODO: Fetch from role

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
                null,
                permissions,
                tenant.Id.ToString(),
                tenant.Name
            )
        );
    }
}
