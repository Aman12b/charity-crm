using CharityCrm.Core.Abstractions;
using Microsoft.Extensions.Options;

namespace CharityCrm.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string Section = "Storage";

    public string InvoicePath { get; set; } = "/data/invoices";
}

/// <summary>Stores invoice PDFs as files below <see cref="StorageOptions.InvoicePath"/>.</summary>
public sealed class FileInvoiceStorage(IOptions<StorageOptions> options) : IInvoiceStorage
{
    public async Task SaveAsync(string relativePath, byte[] content, CancellationToken ct = default)
    {
        var path = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content, ct);
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var path = Resolve(relativePath);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, ct) : null;
    }

    private string Resolve(string relativePath)
    {
        var root = Path.GetFullPath(options.Value.InvoicePath);
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!full.StartsWith(root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invoice path escapes the storage root.");
        }
        return full;
    }
}
