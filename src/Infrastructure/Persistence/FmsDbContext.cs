using System.Text.Json;
using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FMS.Infrastructure.Persistence;

public class FmsDbContext : DbContext
{
    public FmsDbContext(DbContextOptions<FmsDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceVendor> DeviceVendors => Set<DeviceVendor>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // JSON converter for Dictionary<string, object> properties
        var jsonConverter = new ValueConverter<Dictionary<string, object>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)!);

        var jsonConverterNonNullable = new ValueConverter<Dictionary<string, object>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v) ? new Dictionary<string, object>() : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null)!);

        // Configure all Dictionary<string, object> properties across entities
        modelBuilder.Entity<Tenant>().Property(t => t.Settings).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<User>().Property(u => u.Preferences).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<Vehicle>().Property(v => v.Metadata).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<Driver>().Property(d => d.Documents).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<Device>().Property(d => d.Config).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<DeviceVendor>().Property(d => d.SchemaConfig).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<DeviceCommand>().Property(d => d.Payload).HasConversion(jsonConverter);
        modelBuilder.Entity<DeviceCommand>().Property(d => d.ResponsePayload).HasConversion(jsonConverter);
        modelBuilder.Entity<Feature>().Property(f => f.Config).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<UserPreference>().Property(u => u.Config).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<Role>().Property(r => r.Permissions).HasConversion(
            new ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!));

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FmsDbContext).Assembly);
    }
}
