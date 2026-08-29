using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMS.Infrastructure.Repositories;

public class TenantRepository : GenericRepository<Tenant>, ITenantRepository
{
    public TenantRepository(FmsDbContext context) : base(context) { }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Subdomain == subdomain);
    }

    public async Task<bool> IsSubdomainAvailableAsync(string subdomain)
    {
        return !await _dbSet.AnyAsync(t => t.Subdomain == subdomain);
    }
}
