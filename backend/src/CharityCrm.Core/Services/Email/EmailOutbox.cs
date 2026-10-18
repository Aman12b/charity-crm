using CharityCrm.Core.Abstractions;
using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Services.Email;

public static class EmailOutbox
{
    /// <summary>Adds an outbox message for the invoice to the current unit of work and marks the invoice pending.</summary>
    public static EmailOutboxMessage Enqueue(IAppDbContext db, Invoice invoice, string toAddress, DateTimeOffset now)
    {
        var message = new EmailOutboxMessage
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Invoice = invoice,
            ToAddress = toAddress,
            Status = OutboxStatus.Pending,
            NextAttemptAt = now,
            CreatedAt = now,
        };
        db.EmailOutboxMessages.Add(message);

        invoice.EmailStatus = EmailStatus.Pending;
        invoice.EmailLastError = null;
        return message;
    }
}
