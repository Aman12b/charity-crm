using System.Collections.Concurrent;
using CharityCrm.Core.Abstractions;

namespace CharityCrm.Api.IntegrationTests.Infrastructure;

/// <summary>Records sent emails instead of talking to SMTP; can simulate failures per recipient.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();
    private readonly ConcurrentDictionary<string, bool> _failingRecipients = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<EmailMessage> SentTo(string address) =>
        _sent.Where(m => string.Equals(m.ToAddress, address, StringComparison.OrdinalIgnoreCase)).ToList();

    public void FailFor(string address) => _failingRecipients[address] = true;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (_failingRecipients.ContainsKey(message.ToAddress))
        {
            throw new InvalidOperationException("SMTP-Server nicht erreichbar");
        }
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }
}
