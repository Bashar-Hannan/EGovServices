using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("MedicalRecords");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.CitizenNationalNumber)
            .IsRequired()
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(x => x.BloodType)
            .IsRequired()
            .HasMaxLength(5)
            .IsUnicode(false);

        builder.Property(x => x.HeightCm)
            .IsRequired();

        builder.Property(x => x.WeightKg)
            .IsRequired();

        builder.Property(x => x.Allergies)
            .IsRequired()
            .HasMaxLength(500)
            .IsUnicode(true);

        builder.Property(x => x.ChronicDiseases)
            .IsRequired()
            .HasMaxLength(500)
            .IsUnicode(true);

        builder.Property(x => x.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // FK → Citizens — سجل طبي واحد لكل مواطن
        builder.HasOne(x => x.Citizen)
            .WithMany()
            .HasForeignKey(x => x.CitizenNationalNumber)
            .HasPrincipalKey(c => c.NationalNumber)
            .OnDelete(DeleteBehavior.Restrict);
        // WHY Restrict: نفس منطق CriminalRecord — لا يُحذف السجل الطبي
        // تلقائياً عند حذف المواطن (سجلات تاريخية يجب الحفاظ عليها)

        // مواطن واحد = سجل طبي واحد فقط
        builder.HasIndex(x => x.CitizenNationalNumber)
            .IsUnique()
            .HasDatabaseName("IX_MedicalRecords_NationalNumber");
    }
}
