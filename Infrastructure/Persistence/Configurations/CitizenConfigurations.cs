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
        builder.Property(x => x.NationalNumber).IsRequired().HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100).IsUnicode(true);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100).IsUnicode(true);
        builder.Property(x => x.FatherName).IsRequired().HasMaxLength(100).IsUnicode(true);
        builder.Property(x => x.MotherName).IsRequired().HasMaxLength(100).IsUnicode(true);
        builder.Property(x => x.Gender).IsRequired().HasMaxLength(10).IsUnicode(false);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.BirthDate).IsRequired().HasColumnType("date");
        builder.Property(x => x.Email).HasMaxLength(200).IsUnicode(false).IsRequired(false);

        builder.HasMany(x => x.Phones)
            .WithOne(x => x.Citizen)
            .HasForeignKey(x => x.CitizenNationalNumber)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CitizenPhoneConfiguration : IEntityTypeConfiguration<CitizenPhone>
{
    public void Configure(EntityTypeBuilder<CitizenPhone> builder)
    {
        builder.ToTable("CitizenPhones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.Number).IsRequired().HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.CitizenNationalNumber).IsRequired().HasMaxLength(20).IsUnicode(false);

        builder.HasOne(x => x.Citizen)
            .WithMany(x => x.Phones)
            .HasForeignKey(x => x.CitizenNationalNumber)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CitizenNationalNumber).HasDatabaseName("IX_CitizenPhones_NationalNumber");
    }
}
