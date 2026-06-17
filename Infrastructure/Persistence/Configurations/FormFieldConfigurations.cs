using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class ServiceFormFieldConfiguration : IEntityTypeConfiguration<ServiceFormField>
{
    public void Configure(EntityTypeBuilder<ServiceFormField> builder)
    {
        builder.ToTable("ServiceFormFields");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.GovernmentServiceId).IsRequired();
        builder.Property(x => x.FieldName).IsRequired().HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(200).IsUnicode(true);
        builder.Property(x => x.FieldType).IsRequired().HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.IsRequired).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.ValidationRules).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.Placeholder).HasMaxLength(200).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.DefaultValue).HasMaxLength(500).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.HelpText).HasMaxLength(500).IsUnicode(true).IsRequired(false);
        builder.Property(x => x.Metadata).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.GovernmentService)
            .WithMany(x => x.FormFields)
            .HasForeignKey(x => x.GovernmentServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Options)
            .WithOne(x => x.ServiceFormField)
            .HasForeignKey(x => x.ServiceFormFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GovernmentServiceId, x.IsActive, x.DisplayOrder })
            .HasDatabaseName("IX_ServiceFormFields_ServiceId_Active_Order").HasFilter("[IsActive] = 1");
        builder.HasIndex(x => new { x.GovernmentServiceId, x.FieldName })
            .HasDatabaseName("IX_ServiceFormFields_ServiceId_FieldName").IsUnique();
    }
}

public class ServiceFieldOptionConfiguration : IEntityTypeConfiguration<ServiceFieldOption>
{
    public void Configure(EntityTypeBuilder<ServiceFieldOption> builder)
    {
        builder.ToTable("ServiceFieldOptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.ServiceFormFieldId).IsRequired();
        builder.Property(x => x.OptionValue).IsRequired().HasMaxLength(100).IsUnicode(false);
        builder.Property(x => x.OptionLabel).IsRequired().HasMaxLength(200).IsUnicode(true);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasOne(x => x.ServiceFormField)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.ServiceFormFieldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ServiceFormFieldId, x.IsActive, x.DisplayOrder })
            .HasDatabaseName("IX_ServiceFieldOptions_FieldId_Active_Order").HasFilter("[IsActive] = 1");
        builder.HasIndex(x => new { x.ServiceFormFieldId, x.OptionValue })
            .HasDatabaseName("IX_ServiceFieldOptions_FieldId_Value").IsUnique();
    }
}
