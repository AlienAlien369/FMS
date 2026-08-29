using FMS.Domain.Entities;

namespace FMS.Domain.Interfaces;

public interface IDeviceRepository : IGenericRepository<Device>
{
    Task<IReadOnlyList<Device>> GetByTenantIdAsync(Guid tenantId);
    Task<Device?> GetByImeiAsync(string imei);
    Task<IReadOnlyList<Device>> GetOfflineDevicesAsync(Guid tenantId, TimeSpan threshold);
}
