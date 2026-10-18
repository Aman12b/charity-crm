namespace CharityCrm.Core.Abstractions;

public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);

public sealed record EmailMessage(
    string ToAddress,
    string? ToName,
    string Subject,
    string HtmlBody,
    string TextBody,
    IReadOnlyList<EmailAttachment> Attachments);

/// <summary>Sends one email; throws on failure. Implemented with MailKit (NFR-OPS-05).</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
