namespace CharityCrm.Core.Abstractions;

/// <summary>PostgreSQL row locks used inside a transaction (NFR-REL-03).</summary>
public interface IDbLocks
{
    /// <summary>Locks the member row so concurrent payments for the same member are serialized (overlap check).</summary>
    Task LockMemberAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>
    /// Increments the year's counter under a row lock and returns the new value. A rollback also rolls back
    /// the increment, so numbers stay gap-free (FR-INV-01).
    /// </summary>
    Task<int> NextInvoiceNumberAsync(int year, CancellationToken ct = default);

    /// <summary>
    /// Locks up to <paramref name="batchSize"/> due outbox messages (FOR UPDATE SKIP LOCKED) and returns their ids,
    /// so several workers never send the same message twice.
    /// </summary>
    Task<IReadOnlyList<Guid>> ClaimDueOutboxMessagesAsync(DateTimeOffset now, int batchSize, CancellationToken ct = default);
}
