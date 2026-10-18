using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharityCrm.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public const string MemberNumberSequence = "member_number_seq";

    public void Configure(EntityTypeBuilder<Member> b)
    {
        b.Property(m => m.MemberNumber)
            .HasMaxLength(10)
            .HasDefaultValueSql($"'M-' || lpad(nextval('{MemberNumberSequence}')::text, 6, '0')")
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Throw); // immutable (FR-MEM-01)
        b.HasIndex(m => m.MemberNumber).IsUnique();

        b.Property(m => m.FirstName).HasMaxLength(100).IsRequired();
        b.Property(m => m.LastName).HasMaxLength(100).IsRequired();
        b.Property(m => m.Street).HasMaxLength(200).IsRequired();
        b.Property(m => m.PostalCode).HasMaxLength(10).IsRequired();
        b.Property(m => m.City).HasMaxLength(100).IsRequired();
        b.Property(m => m.Country).HasMaxLength(100).IsRequired().HasDefaultValue(Member.DefaultCountry);
        b.Property(m => m.Email).HasMaxLength(254);
        b.Property(m => m.Phone).HasMaxLength(30);
        b.Property(m => m.BillingInterval).HasConversion<string>().HasMaxLength(10);
        b.Property(m => m.Status).HasConversion<string>().HasMaxLength(10);
        b.Property(m => m.LeaveReason).HasMaxLength(500);
        b.Property(m => m.Notes).HasMaxLength(2000);
        b.Property(m => m.RowVersion).IsRowVersion();

        // Case-insensitive partial search (ILIKE '%term%') is served by trigram indexes (NFR-PERF-03).
        b.HasIndex(m => m.LastName).HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasIndex(m => m.FirstName).HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasIndex(m => m.Email).HasMethod("gin").HasOperators("gin_trgm_ops");
        b.HasIndex(m => new { m.Status, m.BillingInterval });
        b.HasIndex(m => new { m.LastName, m.FirstName, m.DateOfBirth });
    }
}
