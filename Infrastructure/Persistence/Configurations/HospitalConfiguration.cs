using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class HospitalConfiguration : IEntityTypeConfiguration<Hospital>
{
    public void Configure(EntityTypeBuilder<Hospital> builder)
    {
        builder.ToTable("Hospitals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.GovernmentEntityId).IsRequired();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(300).IsUnicode(true);
        builder.Property(x => x.Governorate).IsRequired().HasMaxLength(100).IsUnicode(true);
        builder.Property(x => x.Address).HasMaxLength(500).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.Specialty).HasMaxLength(150).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.InfrastructureStatus).HasMaxLength(100).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.OperationalStatus).HasMaxLength(100).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.BedsActual).IsRequired(false);
        builder.Property(x => x.BedsTheoretical).IsRequired(false);
        builder.Property(x => x.PhoneNumber).HasMaxLength(100).IsUnicode(false).IsRequired(false);
        builder.Property(x => x.AdminPhoneNumber).HasMaxLength(100).IsUnicode(false).IsRequired(false);
        builder.Property(x => x.DirectorPhoneNumber).HasMaxLength(150).IsUnicode(false).IsRequired(false);

        // نفس شكل خطوط الطول والعرض الموجودة في Branch (وزارة الداخلية)
        builder.Property(x => x.Latitude).HasColumnType("decimal(10,7)").IsRequired(false);
        builder.Property(x => x.Longitude).HasColumnType("decimal(10,7)").IsRequired(false);

        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.GovernmentEntity)
            .WithMany(x => x.Hospitals)
            .HasForeignKey(x => x.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        // فلترة سريعة حسب المحافظة والاختصاص — نفس الفلاتر التي يطلبها الفرونت
        builder.HasIndex(x => new { x.GovernmentEntityId, x.Governorate, x.IsActive })
            .HasDatabaseName("IX_Hospitals_EntityId_Governorate_Active");

        builder.HasIndex(x => x.Specialty)
            .HasDatabaseName("IX_Hospitals_Specialty");
    }
}
