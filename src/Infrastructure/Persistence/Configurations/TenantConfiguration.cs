using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMS.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(255);
        builder.Property(t => t.Subdomain).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.Subdomain).IsUnique();
        builder.Property(t => t.CountryCode).IsRequired().HasMaxLength(2);
        builder.Property(t => t.Timezone).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3);
        builder.Property(t => t.Plan).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Status).IsRequired().HasMaxLength(20);
        builder.Property(t => t.DataResidencyRegion).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Settings).HasColumnType("jsonb");
    }
}
