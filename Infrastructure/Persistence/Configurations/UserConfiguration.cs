using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

/// <summary>
/// Updated UserConfiguration.
/// Adds Email and IsVerified columns.
/// Replace your existing 18_UserConfiguration.cs with this.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.NationalNumber)
            .IsRequired().HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.PhoneNumber)
            .IsRequired().HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.PasswordHash)
            .IsRequired().HasMaxLength(500).IsUnicode(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired().HasColumnType("datetime2(7)");

        builder.Property(x => x.IsActive)
            .IsRequired().HasDefaultValue(true);

        builder.Property(x => x.Role)
            .IsRequired().HasMaxLength(50).IsUnicode(false);

        // ?? NEW ??????????????????????????????????????????????????????

        builder.Property(x => x.Email)
            .HasMaxLength(200)
            .IsUnicode(false)
            .IsRequired(false); // nullable ? old records not broken

        builder.Property(x => x.IsVerified)
            .IsRequired()
            .HasDefaultValue(true);
        // WHY default true:
        // Old seed-data records have no email so we skip verification for them.
        // Only accounts created via new Register flow start as false.

        // ?? Relationships ?????????????????????????????????????????????

        builder.HasOne(x => x.Citizen)
            .WithOne(x => x.User)
            .HasForeignKey<User>(x => x.NationalNumber)
            .HasPrincipalKey<Citizen>(x => x.NationalNumber)
            .OnDelete(DeleteBehavior.Restrict);

        // ?? Indexes ???????????????????????????????????????????????????

        builder.HasIndex(x => x.NationalNumber)
            .HasDatabaseName("IX_Users_NationalNumber").IsUnique();

        builder.HasIndex(x => x.PhoneNumber)
            .HasDatabaseName("IX_Users_PhoneNumber").IsUnique();

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_Users_Email")
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL"); // unique only when not null
    }
}
