using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CharityCrm.Core.Services.Email;

/// <summary>
/// Sends due outbox messages (FR-MAIL-01/02): success marks the invoice "Sent"; a failure is retried with
/// exponential backoff (2^attempts minutes) and marked "Failed" after <see cref="EmailOutboxMessage.MaxAttempts"/> tries.
/// </summary>
public sealed class EmailOutboxProcessor(
    IAppDbContext db,
    IDbLocks locks,
    IEmailSender sender,
    IInvoiceStorage storage,
    IInvoicePdfRenderer pdf,
    IClock clock,
    ILogger<EmailOutboxProcessor> logger)
{
    public const int BatchSize = 10;

    /// <returns>Number of messages processed (sent or failed attempt).</returns>
    public async Task<int> ProcessBatchAsync(CancellationToken ct = default)
    {
        await using var tx = await db.BeginTransactionAsync(ct);
        var ids = await locks.ClaimDueOutboxMessagesAsync(clock.UtcNow, BatchSize, ct);
        if (ids.Count == 0)
        {
            return 0;
        }

        var messages = await db.EmailOutboxMessages
            .Include(m => m.Invoice).ThenInclude(i => i.Payment)
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            await ProcessAsync(message, ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return messages.Count;
    }

    /// <summary>Delay before the next attempt after <paramref name="attempts"/> failed attempts.</summary>
    public static TimeSpan RetryDelay(int attempts) => TimeSpan.FromMinutes(Math.Pow(2, attempts));

    private async Task ProcessAsync(EmailOutboxMessage message, CancellationToken ct)
    {
        var invoice = message.Invoice;
        try
        {
            var attachment = await storage.ReadAsync(invoice.PdfPath, ct) ?? pdf.Render(invoice);
            var (html, text) = InvoiceEmailTemplate.Body(invoice);
            await sender.SendAsync(new EmailMessage(
                message.ToAddress,
                $"{invoice.MemberSnapshot.FirstName} {invoice.MemberSnapshot.LastName}",
                InvoiceEmailTemplate.Subject(invoice),
                html,
                text,
                [new EmailAttachment($"Rechnung_{invoice.InvoiceNumber}.pdf", "application/pdf", attachment)]), ct);

            var now = clock.UtcNow;
            message.Attempts++;
            message.Status = OutboxStatus.Sent;
            message.ProcessedAt = now;
            message.LastError = null;
            invoice.EmailStatus = EmailStatus.Sent;
            invoice.EmailSentAt = now;
            invoice.EmailLastError = null;
            logger.LogInformation("Invoice email {OutboxMessageId} for invoice {InvoiceId} sent", message.Id, invoice.Id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            message.Attempts++;
            message.LastError = Truncate(ex.Message, 1000);
            invoice.EmailLastError = message.LastError;

            if (message.Attempts >= EmailOutboxMessage.MaxAttempts)
            {
                message.Status = OutboxStatus.Failed;
                message.ProcessedAt = clock.UtcNow;
                invoice.EmailStatus = EmailStatus.Failed;
                logger.LogWarning("Invoice email {OutboxMessageId} failed permanently after {Attempts} attempts",
                    message.Id, message.Attempts);
            }
            else
            {
                message.NextAttemptAt = clock.UtcNow + RetryDelay(message.Attempts);
                logger.LogWarning("Invoice email {OutboxMessageId} failed (attempt {Attempts}), retry at {NextAttemptAt}",
                    message.Id, message.Attempts, message.NextAttemptAt);
            }
        }
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
