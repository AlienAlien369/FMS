using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMS.Infrastructure.Persistence.Configurations;

public class DeviceVendorConfiguration : IEntityTypeConfiguration<DeviceVendor>
{
    public void Configure(EntityTypeBuilder<DeviceVendor> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(v => v.Code).IsUnique();
        builder.Property(v => v.Protocol).IsRequired().HasMaxLength(20);
        builder.Property(v => v.AdapterVersion).HasMaxLength(20);
        builder.Property(v => v.SchemaConfig).HasColumnType("jsonb");
    }
}
