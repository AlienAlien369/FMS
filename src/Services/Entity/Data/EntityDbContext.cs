using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FMS.Entity.Data;

public class EntityDbContext : DbContext
{
    public EntityDbContext(DbContextOptions<EntityDbContext> options) : base(options) { }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceVendor> DeviceVendors => Set<DeviceVendor>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant-scoped indexes
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasIndex(v => new { v.TenantId, v.VehicleNumber }).IsUnique();
            e.Property(v => v.VehicleNumber).HasMaxLength(50);
        });

        modelBuilder.Entity<Driver>(e =>
        {
            e.HasIndex(d => new { d.TenantId, d.LicenseNumber }).IsUnique();
        });

        modelBuilder.Entity<Device>(e =>
        {
            e.HasIndex(d => new { d.TenantId, d.Imei }).IsUnique();
        });

        // ── Tenant isolation via Global Query Filters ──
        modelBuilder.Entity<Vehicle>().HasQueryFilter(v => EF.Property<Guid>(v, "TenantId") == _currentTenantId);
        modelBuilder.Entity<Driver>().HasQueryFilter(d => EF.Property<Guid>(d, "TenantId") == _currentTenantId);
        modelBuilder.Entity<Device>().HasQueryFilter(d => EF.Property<Guid>(d, "TenantId") == _currentTenantId);
    }

    // Set by middleware before query execution
    private Guid _currentTenantId;

    public void SetCurrentTenant(Guid tenantId) => _currentTenantId = tenantId;
}
