using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CharityCrm.Infrastructure.Persistence.Configurations;

public sealed class OrganizationSettingsConfiguration : IEntityTypeConfiguration<OrganizationSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationSettings> b)
    {
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.Name).HasMaxLength(200).IsRequired();
        b.Property(s => s.Street).HasMaxLength(200).IsRequired();
        b.Property(s => s.PostalCode).HasMaxLength(10).IsRequired();
        b.Property(s => s.City).HasMaxLength(100).IsRequired();
        b.Property(s => s.Country).HasMaxLength(100).IsRequired();
        b.Property(s => s.Email).HasMaxLength(254);
        b.Property(s => s.Phone).HasMaxLength(30);
        b.Property(s => s.Iban).HasMaxLength(34);
        b.Property(s => s.Bic).HasMaxLength(11);
        b.Property(s => s.ZvrNumber).HasMaxLength(20);
        b.Property(s => s.InvoiceFooterText).HasMaxLength(1000);
        b.Property(s => s.MonthlyFee).HasPrecision(10, 2);
        b.Property(s => s.YearlyFee).HasPrecision(10, 2);

        // Default row; the admin replaces the placeholders on the settings page.
        b.HasData(new OrganizationSettings
        {
            Id = OrganizationSettings.SingletonId,
            Name = "Mein Verein",
            Street = "Musterstraße 1",
            PostalCode = "1010",
            City = "Wien",
            Country = Member.DefaultCountry,
            InvoiceFooterText = "Vielen Dank für Ihre Unterstützung!",
            MonthlyFee = 10.00m,
            YearlyFee = 120.00m,
            UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(p => p.Interval).HasConversion<string>().HasMaxLength(10);
        b.Property(p => p.Amount).HasPrecision(10, 2);
        b.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Reference).HasMaxLength(200);
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(10);

        b.HasOne(p => p.Member).WithMany().HasForeignKey(p => p.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<AppUser>().WithMany().HasForeignKey(p => p.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(p => new { p.MemberId, p.PeriodStart });
        b.HasIndex(p => p.PaymentDate);
        b.HasIndex(p => new { p.PeriodStart, p.PeriodEnd });
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.Property(i => i.InvoiceNumber).HasMaxLength(12).IsRequired();
        b.HasIndex(i => i.InvoiceNumber).IsUnique();

        b.HasOne(i => i.Payment).WithOne(p => p.Invoice).HasForeignKey<Invoice>(i => i.PaymentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(i => i.PaymentId).IsUnique();
        b.HasOne<Member>().WithMany().HasForeignKey(i => i.MemberId).OnDelete(DeleteBehavior.Restrict);

        b.Property(i => i.Amount).HasPrecision(10, 2);
        b.Property(i => i.Status).HasConversion<string>().HasMaxLength(10);
        b.Property(i => i.CancellationReason).HasMaxLength(500);
        b.Property(i => i.PdfPath).HasMaxLength(300).IsRequired();
        b.Property(i => i.EmailStatus).HasConversion<string>().HasMaxLength(15);
        b.Property(i => i.EmailLastError).HasMaxLength(1000);

        // Snapshots are stored as jsonb (ARCHITECTURE §4).
        b.OwnsOne(i => i.MemberSnapshot, s => s.ToJson());
        b.OwnsOne(i => i.OrganizationSnapshot, s => s.ToJson());

        b.HasIndex(i => i.IssueDate);
        b.HasIndex(i => i.MemberId);
        b.HasIndex(i => new { i.Status, i.EmailStatus });
    }
}

public sealed class InvoiceNumberSequenceConfiguration : IEntityTypeConfiguration<InvoiceNumberSequence>
{
    public void Configure(EntityTypeBuilder<InvoiceNumberSequence> b)
    {
        b.HasKey(s => s.Year);
        b.Property(s => s.Year).ValueGeneratedNever();
    }
}

public sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> b)
    {
        b.HasOne(m => m.Invoice).WithMany().HasForeignKey(m => m.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        b.Property(m => m.ToAddress).HasMaxLength(254).IsRequired();
        b.Property(m => m.Status).HasConversion<string>().HasMaxLength(10);
        b.Property(m => m.LastError).HasMaxLength(1000);
        b.HasIndex(m => new { m.Status, m.NextAttemptAt });
    }
}
