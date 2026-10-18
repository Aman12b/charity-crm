using CharityCrm.Core.Services.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CharityCrm.Infrastructure.Email;

public sealed class EmailWorkerOptions
{
    public const string Section = "Email";

    public bool WorkerEnabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 10;
}

/// <summary>Polls the email outbox every few seconds (ARCHITECTURE §5.1, NFR-REL-02).</summary>
public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopes,
    IOptions<EmailWorkerOptions> options,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
        {
            logger.LogInformation("Email outbox worker is disabled");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));
        do
        {
            try
            {
                // Drain all due messages, one batch per transaction.
                int processed;
                do
                {
                    await using var scope = scopes.CreateAsyncScope();
                    processed = await scope.ServiceProvider.GetRequiredService<EmailOutboxProcessor>()
                        .ProcessBatchAsync(stoppingToken);
                } while (processed == EmailOutboxProcessor.BatchSize && !stoppingToken.IsCancellationRequested);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Email outbox processing failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
