using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Core.Abstractions;

/// <summary>Data access used by Core services; implemented by the EF Core DbContext in Infrastructure.</summary>
public interface IAppDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Member> Members { get; }
    DbSet<OrganizationSettings> OrganizationSettings { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Invoice> Invoices { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Sets the concurrency token the client last saw, so SaveChanges fails on a stale version (NFR-REL-03).</summary>
    void SetOriginalRowVersion<TEntity>(TEntity entity, uint rowVersion) where TEntity : class;

    /// <summary>Starts a database transaction; dispose without commit to roll back.</summary>
    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
}
