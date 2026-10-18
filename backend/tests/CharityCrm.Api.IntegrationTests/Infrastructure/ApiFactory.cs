using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CharityCrm.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the API in-memory against a real PostgreSQL started with Testcontainers.
/// One container is shared by all tests in the "api" collection.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), "charitycrm-tests", Guid.NewGuid().ToString("N"));

    protected virtual IDictionary<string, string?> ExtraSettings => new Dictionary<string, string?>();

    public string ConnectionString => _db.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Storage:InvoicePath", Path.Combine(_dataPath, "invoices"));
        builder.UseSetting("DataProtection:KeyPath", Path.Combine(_dataPath, "keys"));
        foreach (var (key, value) in ExtraSettings)
        {
            builder.UseSetting(key, value);
        }
    }

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
        try { Directory.Delete(_dataPath, recursive: true); } catch (IOException) { }
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
