using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FMS.Config.Data;

public class ConfigDbContext : DbContext
{
    public ConfigDbContext(DbContextOptions<ConfigDbContext> options) : base(options) { }

    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Feature>(e =>
        {
            e.HasIndex(f => new { f.TenantId, f.Module, f.FeatureName }).IsUnique();
        });

        modelBuilder.Entity<UserPreference>(e =>
        {
            e.HasIndex(p => new { p.UserId, p.Page, p.PreferenceType }).IsUnique();
        });
    }
}
