using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.GovernmentServiceId).IsRequired();
        builder.Property(x => x.ReferenceNumber).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.SubmissionDate).IsRequired().HasColumnType("datetime2(7)");
        builder.Property(x => x.BranchId).IsRequired(false);
        builder.Property(x => x.FormData).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.ProcessingNotes).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.CompletedAt).HasColumnType("datetime2(7)").IsRequired(false);
        builder.Property(x => x.RejectionReason).HasMaxLength(500).IsUnicode(true).IsRequired(false);

        builder.HasOne(x => x.User)
            .WithMany(x => x.ServiceRequests)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GovernmentService)
            .WithMany(x => x.ServiceRequests)
            .HasForeignKey(x => x.GovernmentServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany(x => x.ServiceRequests)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.ServiceRequest)
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AuditLogs)
            .WithOne(x => x.ServiceRequest)
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Appointment)
            .WithOne(x => x.ServiceRequest)
            .HasForeignKey<Appointment>(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        builder.HasMany(x => x.WalletTransactions)
            .WithOne(x => x.ServiceRequest)
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(x => x.ReferenceNumber).HasDatabaseName("IX_ServiceRequests_ReferenceNumber").IsUnique();
        builder.HasIndex(x => new { x.UserId, x.SubmissionDate }).HasDatabaseName("IX_ServiceRequests_UserId_SubmissionDate").IsDescending(false, true);
        builder.HasIndex(x => new { x.Status, x.SubmissionDate }).HasDatabaseName("IX_ServiceRequests_Status_SubmissionDate").IsDescending(false, true);
        builder.HasIndex(x => new { x.GovernmentServiceId, x.SubmissionDate }).HasDatabaseName("IX_ServiceRequests_ServiceId_SubmissionDate").IsDescending(false, true);
    }
}
