using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Balance).IsRequired().HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10).IsUnicode(false);
        builder.Property(x => x.LastUpdated).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.IsLocked).IsRequired().HasDefaultValue(false);

        builder.HasOne(x => x.User)
            .WithOne(x => x.Wallet)
            .HasForeignKey<Wallet>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId).HasDatabaseName("IX_Wallets_UserId").IsUnique();
    }
}

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.WalletId).IsRequired();
        builder.Property(x => x.Amount).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.TransactionType).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.Description).HasMaxLength(500).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.ReferenceId).HasMaxLength(100).IsUnicode(false).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.ServiceRequestId).IsRequired(false);

        builder.HasOne(x => x.Wallet)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ServiceRequest)
            .WithMany(x => x.WalletTransactions)
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(x => new { x.WalletId, x.CreatedAt })
            .HasDatabaseName("IX_WalletTransactions_WalletId_CreatedAt").IsDescending(false, true);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200).IsUnicode(true);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(1000).IsUnicode(true);
        builder.Property(x => x.NotificationType).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.IsRead).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("datetime2(7)");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt").HasFilter("[IsRead] = 0");
    }
}

public class FAQConfiguration : IEntityTypeConfiguration<FAQ>
{
    public void Configure(EntityTypeBuilder<FAQ> builder)
    {
        builder.ToTable("FAQ");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.Question).IsRequired().HasMaxLength(500).IsUnicode(true);
        builder.Property(x => x.Answer).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100).IsUnicode(true);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(x => new { x.IsActive, x.DisplayOrder })
            .HasDatabaseName("IX_FAQ_Active_Order").HasFilter("[IsActive] = 1");
    }
}
