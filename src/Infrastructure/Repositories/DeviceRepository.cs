using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMS.Infrastructure.Repositories;

public class DeviceRepository : GenericRepository<Device>, IDeviceRepository
{
    public DeviceRepository(FmsDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Device>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet.Where(d => d.TenantId == tenantId).ToListAsync();
    }

    public async Task<Device?> GetByImeiAsync(string imei)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.Imei == imei);
    }

    public async Task<IReadOnlyList<Device>> GetOfflineDevicesAsync(Guid tenantId, TimeSpan threshold)
    {
        var cutoff = DateTime.UtcNow - threshold;
        return await _dbSet.Where(d => 
            d.TenantId == tenantId && 
            (d.LastSeen == null || d.LastSeen < cutoff))
            .ToListAsync();
    }
}
