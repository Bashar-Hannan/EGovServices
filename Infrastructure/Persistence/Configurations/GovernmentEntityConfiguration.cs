using EGovServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EGovServices.Infrastructure.Persistence.Configurations;

public class GovernmentEntityConfiguration : IEntityTypeConfiguration<GovernmentEntity>
{
    public void Configure(EntityTypeBuilder<GovernmentEntity> builder)
    {
        builder.ToTable("GovernmentEntities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200).IsUnicode(true);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000).IsUnicode(true);
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasMany(x => x.Branches)
            .WithOne(x => x.GovernmentEntity)
            .HasForeignKey(x => x.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.GovernmentServices)
            .WithOne(x => x.GovernmentEntity)
            .HasForeignKey(x => x.GovernmentEntityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
