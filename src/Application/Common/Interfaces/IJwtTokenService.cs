using FMS.Domain.Entities;

namespace FMS.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, Tenant tenant, List<string> permissions);
    string GenerateRefreshToken();
    bool ValidateRefreshToken(string token);
}
