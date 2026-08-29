using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMS.Infrastructure.Repositories;

public class DeviceVendorRepository : GenericRepository<DeviceVendor>, IDeviceVendorRepository
{
    public DeviceVendorRepository(FmsDbContext context) : base(context) { }

    public async Task<DeviceVendor?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(v => v.Code == code);
    }

    public async Task<IReadOnlyList<DeviceVendor>> GetActiveVendorsAsync()
    {
        return await _dbSet.Where(v => v.IsActive).ToListAsync();
    }
}
