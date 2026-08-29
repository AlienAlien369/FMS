using FMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMS.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VehicleNumber).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Type).HasMaxLength(50);
        builder.Property(v => v.Model).HasMaxLength(100);
        builder.Property(v => v.FuelType).HasMaxLength(20);
        builder.Property(v => v.Status).HasMaxLength(20);
        builder.Property(v => v.Metadata).HasColumnType("jsonb");

        builder.HasIndex(v => v.TenantId);
        builder.HasIndex(v => new { v.TenantId, v.Status });

        builder.HasOne(v => v.Tenant)
            .WithMany(t => t.Vehicles)
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
