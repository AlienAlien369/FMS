using FMS.Domain.Entities;

namespace FMS.Domain.Interfaces;

public interface IDeviceVendorRepository : IGenericRepository<DeviceVendor>
{
    Task<DeviceVendor?> GetByCodeAsync(string code);
    Task<IReadOnlyList<DeviceVendor>> GetActiveVendorsAsync();
}
