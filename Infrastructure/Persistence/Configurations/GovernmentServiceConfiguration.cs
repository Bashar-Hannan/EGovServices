using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class GovernmentServiceConfiguration : IEntityTypeConfiguration<GovernmentService>
{
    public void Configure(EntityTypeBuilder<GovernmentService> builder)
    {
        builder.ToTable("GovernmentServices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.GovernmentEntityId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200).IsUnicode(true);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000).IsUnicode(true);
        builder.Property(x => x.Requirements).HasMaxLength(2000).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.ServiceFee).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.GovernmentEntity)
            .WithMany(x => x.GovernmentServices)
            .HasForeignKey(x => x.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.FormFields)
            .WithOne(x => x.GovernmentService)
            .HasForeignKey(x => x.GovernmentServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ServiceSlots)
            .WithOne(x => x.GovernmentService)
            .HasForeignKey(x => x.GovernmentServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ServiceRequests)
            .WithOne(x => x.GovernmentService)
            .HasForeignKey(x => x.GovernmentServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.GovernmentEntityId).HasDatabaseName("IX_GovernmentServices_EntityId");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_GovernmentServices_IsActive").HasFilter("[IsActive] = 1");
    }
}
