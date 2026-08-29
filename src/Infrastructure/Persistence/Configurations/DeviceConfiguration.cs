using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMS.Infrastructure.Persistence.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Imei).HasMaxLength(50);
        builder.Property(d => d.SerialNumber).HasMaxLength(100);
        builder.Property(d => d.Model).HasMaxLength(100);
        builder.Property(d => d.FirmwareVersion).HasMaxLength(50);
        builder.Property(d => d.Status).HasMaxLength(20);
        builder.Property(d => d.Config).HasColumnType("jsonb");

        builder.HasIndex(d => d.TenantId);
        builder.HasIndex(d => d.Imei).IsUnique();
        builder.HasIndex(d => new { d.TenantId, d.Status });

        builder.HasOne(d => d.Tenant)
            .WithMany(t => t.Devices)
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Vendor)
            .WithMany()
            .HasForeignKey(d => d.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Vehicle)
            .WithMany(v => v.Devices)
            .HasForeignKey(d => d.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
