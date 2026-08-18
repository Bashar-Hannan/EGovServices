using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class ElectricityBillConfiguration : IEntityTypeConfiguration<ElectricityBill>
{
    public void Configure(EntityTypeBuilder<ElectricityBill> builder)
    {
        builder.ToTable("ElectricityBills");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.BillNumber).IsRequired().HasMaxLength(30).IsUnicode(false);
        builder.HasIndex(x => x.BillNumber).IsUnique();

        builder.Property(x => x.MeterNumber).IsRequired().HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.CitizenNationalNumber).IsRequired().HasMaxLength(20).IsUnicode(false);
        builder.Property(x => x.Month).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(10,2)");
        builder.Property(x => x.Status).IsRequired().HasMaxLength(10).HasDefaultValue("Unpaid");
        builder.Property(x => x.PaidAt).IsRequired(false).HasColumnType("datetime2");
        builder.Property(x => x.WalletTransactionId).IsRequired(false);

        // FK → Citizens
        builder.HasOne(x => x.Citizen)
            .WithMany()
            .HasForeignKey(x => x.CitizenNationalNumber)
            .HasPrincipalKey(c => c.NationalNumber)
            .OnDelete(DeleteBehavior.Restrict);

        // FK → WalletTransactions
        builder.HasOne(x => x.WalletTransaction)
            .WithMany()
            .HasForeignKey(x => x.WalletTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => x.MeterNumber).HasDatabaseName("IX_ElectricityBills_MeterNumber");
        builder.HasIndex(x => x.CitizenNationalNumber).HasDatabaseName("IX_ElectricityBills_NationalNumber");
        builder.HasIndex(x => new { x.MeterNumber, x.Status })
            .HasDatabaseName("IX_ElectricityBills_MeterNumber_Status")
            .HasFilter("[Status] = 'Unpaid'");
    }
}
