using System.Net;
using System.Net.Http.Json;
using CharityCrm.Api.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace CharityCrm.Api.IntegrationTests.Features;

[Collection(ApiCollection.Name)]
public sealed class HealthTests(ApiFactory factory)
{
    [Fact]
    public async Task NFR_OPS_03_Health_reports_api_and_database_status()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().ContainKey("database").WhoseValue.Should().Be("Healthy");
    }

    private sealed record HealthBody(string Status, Dictionary<string, string> Checks);
}
