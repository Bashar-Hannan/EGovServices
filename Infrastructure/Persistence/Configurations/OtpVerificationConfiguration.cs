using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("OtpVerifications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.NationalNumber)
            .IsRequired().HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.OtpCode)
            .IsRequired().HasMaxLength(6).IsUnicode(false);

        builder.Property(x => x.ExpiresAt)
            .IsRequired().HasColumnType("datetime2(7)");

        builder.Property(x => x.IsUsed)
            .IsRequired().HasDefaultValue(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired().HasColumnType("datetime2(7)");

        builder.Property(x => x.Attempts)
            .IsRequired().HasDefaultValue(0);

        // ── Temp Registration Data ────────────────────────────────────

        builder.Property(x => x.TempPhoneNumber)
            .IsRequired().HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.TempEmail)
            .IsRequired().HasMaxLength(200).IsUnicode(false);

        builder.Property(x => x.TempPasswordHash)
            .IsRequired().HasMaxLength(500).IsUnicode(false);

        // Index: fast OTP lookup per citizen
        builder.HasIndex(x => new { x.NationalNumber, x.IsUsed, x.ExpiresAt })
            .HasDatabaseName("IX_OtpVerifications_NationalNumber_Active");
    }
}
