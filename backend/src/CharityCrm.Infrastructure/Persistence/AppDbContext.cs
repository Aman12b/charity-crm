using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using CharityCrm.Infrastructure.Persistence.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CharityCrm.Infrastructure.Persistence;

/// <summary>Identity users plus the application tables. Roles are a column on the user, so no role tables.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options), IAppDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences => Set<InvoiceNumberSequence>();
    public DbSet<EmailOutboxMessage> EmailOutboxMessages => Set<EmailOutboxMessage>();

    public void SetOriginalRowVersion<TEntity>(TEntity entity, uint rowVersion) where TEntity : class =>
        Entry(entity).Property<uint>("RowVersion").OriginalValue = rowVersion;

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        new EfTransaction(await Database.BeginTransactionAsync(ct));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasSequence<long>(MemberConfiguration.MemberNumberSequence);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private sealed class EfTransaction(IDbContextTransaction inner) : ITransaction
    {
        public Task CommitAsync(CancellationToken ct = default) => inner.CommitAsync(ct);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
