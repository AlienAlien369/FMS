using FMS.Domain.Entities;

namespace FMS.Domain.Interfaces;

public interface ITenantRepository : IGenericRepository<Tenant>
{
    Task<Tenant?> GetBySubdomainAsync(string subdomain);
    Task<bool> IsSubdomainAvailableAsync(string subdomain);
}
