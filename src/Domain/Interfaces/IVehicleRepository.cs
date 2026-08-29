using FMS.Domain.Entities;

namespace FMS.Domain.Interfaces;

public interface IVehicleRepository : IGenericRepository<Vehicle>
{
    Task<IReadOnlyList<Vehicle>> GetByTenantIdAsync(Guid tenantId);
    Task<Vehicle?> GetByVehicleNumberAsync(Guid tenantId, string vehicleNumber);
}
