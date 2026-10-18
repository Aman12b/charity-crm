using CharityCrm.Core.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CharityCrm.Infrastructure.Email;

/// <summary>SMTP transport; Mailpit by default, any relay via configuration only (NFR-OPS-05).</summary>
public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string Host { get; set; } = "mailpit";
    public int Port { get; set; } = 1025;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseStartTls { get; set; }
    public string FromAddress { get; set; } = "noreply@charity.local";
    public string FromName { get; set; } = "Charity CRM";
}

public sealed class MailKitEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var smtp = options.Value;
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName ?? message.ToAddress, message.ToAddress));
        mime.Subject = message.Subject;

        var body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody };
        foreach (var attachment in message.Attachments)
        {
            body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }
        mime.Body = body.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(smtp.Host, smtp.Port,
            smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? "", ct);
        }
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
