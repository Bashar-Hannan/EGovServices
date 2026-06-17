using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class CriminalRecordConfiguration : IEntityTypeConfiguration<CriminalRecord>
{
    public void Configure(EntityTypeBuilder<CriminalRecord> builder)
    {
        builder.ToTable("CriminalRecords");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.CitizenNationalNumber)
            .IsRequired()
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(x => x.CrimeDescription)
            .IsRequired()
            .HasMaxLength(500)
            .IsUnicode(true);

        builder.Property(x => x.JudgmentDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // FK → Citizens
        builder.HasOne(x => x.Citizen)
            .WithMany()
            .HasForeignKey(x => x.CitizenNationalNumber)
            .HasPrincipalKey(c => c.NationalNumber)
            .OnDelete(DeleteBehavior.Restrict);
        // WHY Restrict: deleting a citizen should NOT delete criminal records
        // (historical/legal records must be preserved)

        // Index: fast lookup by national number (most common query)
        builder.HasIndex(x => x.CitizenNationalNumber)
            .HasDatabaseName("IX_CriminalRecords_NationalNumber");

        // Filtered index: only active records (used in clearance check)
        builder.HasIndex(x => new { x.CitizenNationalNumber, x.IsActive })
            .HasDatabaseName("IX_CriminalRecords_NationalNumber_Active")
            .HasFilter("[IsActive] = 1");
    }
}
