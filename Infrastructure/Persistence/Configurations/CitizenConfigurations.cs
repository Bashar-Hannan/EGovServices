using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class CitizenConfiguration : IEntityTypeConfiguration<Citizen>
{
    public void Configure(EntityTypeBuilder<Citizen> builder)
    {
        builder.ToTable("Citizens");

        builder.HasKey(x => x.NationalNumber);

        builder.Property(x => x.NationalNumber)
            .IsRequired()
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.FatherName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.BirthDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.PlaceOfBirth)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.MaritalStatus)
            .IsRequired()
            .HasMaxLength(20);

        // ?? NEW — ÍÞæá ÇáÞíÏ ÇáãÏäí ??????????????????????????????????
        builder.Property(x => x.MotherName)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Religion)
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(x => x.Gender)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(x => x.RecordPlace)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.RecordNumber)
            .HasMaxLength(20)
            .IsRequired(false)
            .IsUnicode(false);

        // ?? ÚáÇÞÉ ãÚ User ????????????????????????????????????????????
        builder.HasOne(x => x.User)
            .WithOne(x => x.Citizen)
            .HasForeignKey<User>(x => x.NationalNumber)
            .HasPrincipalKey<Citizen>(x => x.NationalNumber)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
