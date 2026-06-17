using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.ServiceSlotId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50).IsUnicode(false);

        builder.HasOne(x => x.ServiceRequest)
            .WithOne(x => x.Appointment)
            .HasForeignKey<Appointment>(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ServiceSlot)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.ServiceSlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255).IsUnicode(true);
        builder.Property(x => x.FilePath).IsRequired().HasMaxLength(1000).IsUnicode(false);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.FileType).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.FileSizeBytes).IsRequired();

        builder.HasOne(x => x.ServiceRequest)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ServiceRequestId).HasDatabaseName("IX_Attachments_ServiceRequestId");
    }
}

public class RequestAuditLogConfiguration : IEntityTypeConfiguration<RequestAuditLog>
{
    public void Configure(EntityTypeBuilder<RequestAuditLog> builder)
    {
        builder.ToTable("RequestAuditLogs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(x => x.OldStatus)
            .HasMaxLength(50)
            .IsRequired(false);      // null Ýí ÇáÓÌá ÇáÃæá ÚäÏ ÇáÊÞÏíã

        builder.Property(x => x.NewStatus)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // ?? ÇáÚáÇÞÉ ãÚ ServiceRequest ?????????????????????????????????
        builder.HasOne(x => x.ServiceRequest)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.ServiceRequestId)
            .OnDelete(DeleteBehavior.Cascade);   // áæ ÍõÐÝ ÇáØáÈ ÊõÍÐÝ ÓÌáÇÊå

        // ?? Index ááÃÏÇÁ — ÇáÇÓÊÚáÇã ÏÇÆãÇð ÈÜ ServiceRequestId ????????
        builder.HasIndex(x => x.ServiceRequestId);

        // ?? Index ãÑßøÈ ááÜ Timeline ÇáãÑÊøÈ ????????????????????????????
        builder.HasIndex(x => new { x.ServiceRequestId, x.CreatedAt });
    }
}
