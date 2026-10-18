using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharityCrm.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(a => a.Id).UseIdentityAlwaysColumn();
        b.Property(a => a.Action).HasMaxLength(50).IsRequired();
        b.Property(a => a.EntityType).HasMaxLength(50);
        b.Property(a => a.EntityId).HasMaxLength(50);
        b.Property(a => a.Changes).HasColumnType("jsonb");

        b.HasOne<AppUser>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(a => a.Timestamp);
        b.HasIndex(a => a.UserId);
        b.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
