using CharityCrm.Core.Entities;
using CharityCrm.Core.Services.Invoices;
using CharityCrm.Core.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CharityCrm.Infrastructure.Persistence;

/// <summary>
/// Development/performance seed (NFR-PERF-01..03): <c>dotnet CharityCrm.Api.dll seed-demo [members] [payments]</c>.
/// Runs only on a database without members. Invoices get no stored PDF; it is rendered on first download.
/// </summary>
public static class DemoDataSeeder
{
    private const int BatchSize = 2_000;

    private static readonly string[] FirstNames =
        ["Anna", "Lukas", "Sophie", "David", "Laura", "Felix", "Lena", "Paul", "Hannah", "Jakob", "Marie", "Elias", "Emma", "Tobias", "Julia"];
    private static readonly string[] LastNames =
        ["Gruber", "Huber", "Wagner", "Müller", "Pichler", "Steiner", "Moser", "Mayer", "Hofer", "Leitner", "Berger", "Fuchs", "Eder", "Fischer", "Schmid"];
    private static readonly string[] Cities = ["Wien", "Graz", "Linz", "Salzburg", "Innsbruck", "Klagenfurt", "St. Pölten", "Bregenz"];

    public static async Task SeedAsync(IServiceProvider services, int memberCount, int paymentCount, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DemoDataSeeder));
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        if (await db.Members.AnyAsync(ct))
        {
            logger.LogWarning("Demo seed skipped: the database already contains members");
            return;
        }
        var recordedBy = await db.Users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).FirstAsync(ct);
        var settings = await db.OrganizationSettings.AsNoTracking().SingleAsync(ct);
        var random = new Random(42);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;

        // Members
        for (var offset = 0; offset < memberCount; offset += BatchSize)
        {
            for (var i = offset; i < Math.Min(offset + BatchSize, memberCount); i++)
            {
                var first = FirstNames[random.Next(FirstNames.Length)];
                var last = LastNames[random.Next(LastNames.Length)];
                db.Members.Add(new Member
                {
                    Id = Guid.NewGuid(),
                    FirstName = first,
                    LastName = last,
                    DateOfBirth = new DateOnly(1940 + random.Next(65), 1 + random.Next(12), 1 + random.Next(28)),
                    Street = $"Hauptstraße {1 + random.Next(200)}",
                    PostalCode = (1010 + random.Next(8900)).ToString(),
                    City = Cities[random.Next(Cities.Length)],
                    Country = Member.DefaultCountry,
                    Email = $"{first}.{last}.{i}@example.at".ToLowerInvariant().Replace("ü", "ue"),
                    BillingInterval = i % 4 == 0 ? BillingInterval.Yearly : BillingInterval.Monthly,
                    JoinDate = new DateOnly(2005, 1, 1),
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.ChangeTracker.DetectChanges();
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("Seeded {Count} members", memberCount);

        // Payments with invoices: consecutive periods per member, ending in the past.
        var members = await db.Members.AsNoTracking().OrderBy(m => m.MemberNumber).ToListAsync(ct);
        var perMember = Math.Max(1, paymentCount / Math.Max(1, members.Count));
        var numbers = new Dictionary<int, int>();
        var created = 0;
        foreach (var member in members)
        {
            for (var k = 0; k < perMember && created < paymentCount; k++, created++)
            {
                var period = member.BillingInterval == BillingInterval.Monthly
                    ? ContributionPeriod.Month(today.AddMonths(-(perMember - k)).Year, today.AddMonths(-(perMember - k)).Month)
                    : ContributionPeriod.Year(today.Year - (perMember - k));
                var paymentDate = period.Start.AddDays(5) > today ? today : period.Start.AddDays(5);
                var amount = member.BillingInterval == BillingInterval.Monthly ? settings.MonthlyFee : settings.YearlyFee;
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    MemberId = member.Id,
                    Interval = member.BillingInterval,
                    PeriodStart = period.Start,
                    PeriodEnd = period.End,
                    Amount = amount,
                    PaymentDate = paymentDate,
                    Method = (PaymentMethod)(created % 3),
                    Status = PaymentStatus.Recorded,
                    RecordedByUserId = recordedBy,
                    CreatedAt = now,
                };
                var year = paymentDate.Year;
                numbers[year] = numbers.GetValueOrDefault(year) + 1;
                var invoiceNumber = InvoiceIssuer.FormatNumber(year, numbers[year]);
                db.Payments.Add(payment);
                db.Invoices.Add(new Invoice
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = invoiceNumber,
                    PaymentId = payment.Id,
                    MemberId = member.Id,
                    IssueDate = paymentDate,
                    Amount = amount,
                    PeriodStart = period.Start,
                    PeriodEnd = period.End,
                    MemberSnapshot = MemberSnapshot.From(member),
                    OrganizationSnapshot = OrganizationSnapshot.From(settings),
                    PdfPath = InvoiceIssuer.PdfPathFor(year, invoiceNumber),
                    EmailStatus = EmailStatus.Sent,
                    EmailSentAt = now,
                    CreatedAt = now,
                });

                if (created % BatchSize == BatchSize - 1)
                {
                    db.ChangeTracker.DetectChanges();
                    await db.SaveChangesAsync(ct);
                    db.ChangeTracker.Clear();
                }
            }
        }
        foreach (var (year, last) in numbers)
        {
            db.InvoiceNumberSequences.Add(new InvoiceNumberSequence { Year = year, LastNumber = last });
        }
        db.ChangeTracker.DetectChanges();
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} payments with invoices", created);
    }
}
