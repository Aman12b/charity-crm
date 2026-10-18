using CharityCrm.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CharityCrm.Infrastructure.Persistence;

/// <summary>Parameterized PostgreSQL locking statements (NFR-REL-03, NFR-SEC-04).</summary>
public sealed class DbLocks(AppDbContext db) : IDbLocks
{
    public async Task LockMemberAsync(Guid memberId, CancellationToken ct = default) =>
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "Members" WHERE "Id" = {memberId} FOR UPDATE""", ct);

    public async Task<int> NextInvoiceNumberAsync(int year, CancellationToken ct = default)
    {
        // Create the year's row if missing; a concurrent insert waits and then does nothing.
        await db.Database.ExecuteSqlAsync(
            $"""INSERT INTO "InvoiceNumberSequences" ("Year", "LastNumber") VALUES ({year}, 0) ON CONFLICT ("Year") DO NOTHING""", ct);

        // UPDATE takes the row lock (equivalent to SELECT ... FOR UPDATE) until the payment transaction ends.
        var values = await db.Database.SqlQuery<int>(
                $"""UPDATE "InvoiceNumberSequences" SET "LastNumber" = "LastNumber" + 1 WHERE "Year" = {year} RETURNING "LastNumber" AS "Value" """)
            .ToListAsync(ct);
        return values.Single();
    }
}
