using FMS.Domain.Entities;
using FMS.Domain.Interfaces;
using FMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMS.Infrastructure.Repositories;

public class VehicleRepository : GenericRepository<Vehicle>, IVehicleRepository
{
    public VehicleRepository(FmsDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Vehicle>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet.Where(v => v.TenantId == tenantId).ToListAsync();
    }

    public async Task<Vehicle?> GetByVehicleNumberAsync(Guid tenantId, string vehicleNumber)
    {
        return await _dbSet.FirstOrDefaultAsync(v => 
            v.TenantId == tenantId && v.VehicleNumber == vehicleNumber);
    }
}
