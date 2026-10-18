using CharityCrm.Core.Entities;

namespace CharityCrm.Core.Abstractions;

/// <summary>Renders the invoice PDF from the invoice snapshots and its payment (FR-INV-01, FR-INV-04).</summary>
public interface IInvoicePdfRenderer
{
    /// <param name="invoice">Invoice with <see cref="Invoice.Payment"/> loaded.</param>
    byte[] Render(Invoice invoice);
}

/// <summary>Stores invoice PDFs below the configured invoice path (Docker volume).</summary>
public interface IInvoiceStorage
{
    Task SaveAsync(string relativePath, byte[] content, CancellationToken ct = default);

    /// <summary>Returns null if the file does not exist.</summary>
    Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct = default);
}
