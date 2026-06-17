using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.GovernmentEntityId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200).IsUnicode(true);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.City).IsRequired().HasMaxLength(100).IsUnicode(true);
        builder.Property(x => x.Latitude).HasColumnType("decimal(10,7)").IsRequired(false);
        builder.Property(x => x.Longitude).HasColumnType("decimal(10,7)").IsRequired(false);
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsUnicode(false).IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.GovernmentEntity)
            .WithMany(x => x.Branches)
            .HasForeignKey(x => x.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.GovernmentEntityId, x.IsActive })
            .HasDatabaseName("IX_Branches_EntityId_Active").HasFilter("[IsActive] = 1");
    }
}

public class ServiceSlotConfiguration : IEntityTypeConfiguration<ServiceSlot>
{
    public void Configure(EntityTypeBuilder<ServiceSlot> builder)
    {
        builder.ToTable("ServiceSlots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.GovernmentServiceId).IsRequired();
        builder.Property(x => x.Date).IsRequired().HasColumnType("date");
        builder.Property(x => x.StartTime).IsRequired().HasColumnType("time(0)");
        builder.Property(x => x.EndTime).IsRequired().HasColumnType("time(0)");
        builder.Property(x => x.Capacity).IsRequired();

        builder.HasOne(x => x.GovernmentService)
            .WithMany(x => x.ServiceSlots)
            .HasForeignKey(x => x.GovernmentServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GovernmentServiceId, x.Date })
            .HasDatabaseName("IX_ServiceSlots_ServiceId_Date");
    }
}
