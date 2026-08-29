using System.Text.Json;
using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FMS.Infrastructure.Persistence;

public class FmsDbContext : DbContext
{
    public FmsDbContext(DbContextOptions<FmsDbContext> options) : base(options) { }

    // Existing
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

    // P0: New entities
    public DbSet<Lookup> Lookups => Set<Lookup>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<FormMaster> FormMasters => Set<FormMaster>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<Geofence> Geofences => Set<Geofence>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    // P1: RBAC & Notifications
    public DbSet<FormRoleMapping> FormRoleMappings => Set<FormRoleMapping>();
    public DbSet<FormCompanyMapping> FormCompanyMappings => Set<FormCompanyMapping>();
    public DbSet<FormColumnConfig> FormColumnConfigs => Set<FormColumnConfig>();
    public DbSet<Notification> Notifications => Set<Notification>();

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

        // JSON converter for List<Dictionary<string, object>> (Waypoints)
        var listJsonConverter = new ValueConverter<List<Dictionary<string, object>>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrEmpty(v) ? new List<Dictionary<string, object>>() : JsonSerializer.Deserialize<List<Dictionary<string, object>>>(v, (JsonSerializerOptions?)null)!);

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

        // New entity JSON converters
        modelBuilder.Entity<Lookup>().Property(l => l.Metadata).HasConversion(jsonConverterNonNullable);
        modelBuilder.Entity<Route>().Property(r => r.Waypoints).HasConversion(listJsonConverter);

        // ── Indexes ──

        // Lookup indexes
        modelBuilder.Entity<Lookup>().HasIndex(l => l.Category);
        modelBuilder.Entity<Lookup>().HasIndex(l => l.ParentId);
        modelBuilder.Entity<Lookup>().HasIndex(l => new { l.Category, l.Code }).IsUnique();

        // Client indexes
        modelBuilder.Entity<Client>().HasIndex(c => c.TenantId);
        modelBuilder.Entity<Client>().HasIndex(c => new { c.TenantId, c.ClientCode }).IsUnique();

        // FormMaster indexes
        modelBuilder.Entity<FormMaster>().HasIndex(f => f.FormName).IsUnique();

        // Route indexes
        modelBuilder.Entity<Route>().HasIndex(r => r.TenantId);

        // Geofence indexes
        modelBuilder.Entity<Geofence>().HasIndex(g => g.TenantId);

        // Subscription indexes
        modelBuilder.Entity<Subscription>().HasIndex(s => s.TenantId);

        // FormRoleMapping indexes
        modelBuilder.Entity<FormRoleMapping>().HasIndex(m => new { m.TenantId, m.RoleId, m.FormId }).IsUnique();

        // FormCompanyMapping indexes
        modelBuilder.Entity<FormCompanyMapping>().HasIndex(m => new { m.TenantId, m.FormId }).IsUnique();

        // Notification indexes
        modelBuilder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });

        // AuditLog indexes
        modelBuilder.Entity<AuditLog>().HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId });
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.CreatedAt);

        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FmsDbContext).Assembly);
    }
}
