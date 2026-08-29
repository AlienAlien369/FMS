using FMS.Application.Common.DTOs;
using FMS.Application.Common.Interfaces;
using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using MediatR;

namespace FMS.Application.Features.Tenants.Commands;

public record OnboardTenantCommand(OnboardingRequest Request) : IRequest<TenantResponse>;

public class OnboardTenantHandler : IRequestHandler<OnboardTenantCommand, TenantResponse>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IGenericRepository<User> _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IGenericRepository<Feature> _featureRepository;

    public OnboardTenantHandler(
        ITenantRepository tenantRepository,
        IGenericRepository<User> userRepository,
        IGenericRepository<Role> roleRepository,
        IGenericRepository<Feature> featureRepository)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _featureRepository = featureRepository;
    }

    public async Task<TenantResponse> Handle(OnboardTenantCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        // 1. Create tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = req.CompanyName,
            Subdomain = req.Subdomain,
            CountryCode = req.CountryCode,
            Plan = req.Plan,
            Status = "trial",
            Settings = new Dictionary<string, object>
            {
                ["branding"] = new Dictionary<string, string>
                {
                    ["primaryColor"] = "#1e40af",
                    ["secondaryColor"] = "#3b82f6",
                    ["logoUrl"] = "",
                    ["fontName"] = "Inter"
                }
            }
        };
        await _tenantRepository.AddAsync(tenant);

        // 2. Create default admin role
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Name = "Super Admin",
            Permissions = new List<string>
            {
                "command-center:read", "command-center:write",
                "fleet-intelligence:read", "fleet-intelligence:write",
                "trip-logistics:read", "trip-logistics:write",
                "people-transport:read", "people-transport:write",
                "safety-compliance:read", "safety-compliance:write",
                "analytics:read", "analytics:write",
                "settings:read", "settings:write",
                "device-iot:read", "device-iot:write"
            },
            IsSystemRole = true
        };
        await _roleRepository.AddAsync(adminRole);

        // 3. Create admin user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = req.AdminEmail,
            PasswordHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(req.AdminPassword))),
            FirstName = req.AdminFirstName ?? "Admin",
            LastName = req.AdminLastName,
            RoleId = adminRole.Id
        };
        await _userRepository.AddAsync(adminUser);

        // 4. Enable selected sectors
        foreach (var sector in req.Sectors)
        {
            await _featureRepository.AddAsync(new Feature
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Module = sector,
                FeatureName = $"{sector}:all",
                Enabled = true
            });
        }

        return new TenantResponse(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.CountryCode,
            tenant.Timezone,
            tenant.Currency,
            tenant.Plan,
            tenant.Status,
            tenant.CreatedAt
        );
    }
}
