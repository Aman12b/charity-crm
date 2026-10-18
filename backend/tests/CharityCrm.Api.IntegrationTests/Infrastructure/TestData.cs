using System.Net;
using CharityCrm.Core.Services.Members;

namespace CharityCrm.Api.IntegrationTests.Infrastructure;

public static class TestData
{
    /// <summary>A valid create-member body with unique names, so tests never collide in the shared database.</summary>
    public static Dictionary<string, object?> NewMember(string interval = "Monthly", string? email = "max@example.at")
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        return new Dictionary<string, object?>
        {
            ["firstName"] = $"Max{unique}",
            ["lastName"] = $"Mustermann{unique}",
            ["dateOfBirth"] = "1980-05-17",
            ["street"] = "Hauptstraße 1",
            ["postalCode"] = "1010",
            ["city"] = "Wien",
            ["country"] = null,
            ["email"] = email,
            ["phone"] = "+43 1 234567",
            ["billingInterval"] = interval,
            ["joinDate"] = "2024-01-01",
            ["notes"] = null,
        };
    }

    public static async Task<MemberDto> CreateMemberAsync(this ApiClient client, string interval = "Monthly",
        string? email = "max@example.at") =>
        await client.PostJsonAsync<MemberDto>("/api/members", NewMember(interval, email), HttpStatusCode.Created);

    public static Dictionary<string, object?> ToUpdate(MemberDto m) => new()
    {
        ["firstName"] = m.FirstName,
        ["lastName"] = m.LastName,
        ["dateOfBirth"] = m.DateOfBirth.ToString("yyyy-MM-dd"),
        ["street"] = m.Street,
        ["postalCode"] = m.PostalCode,
        ["city"] = m.City,
        ["country"] = m.Country,
        ["email"] = m.Email,
        ["phone"] = m.Phone,
        ["billingInterval"] = m.BillingInterval.ToString(),
        ["joinDate"] = m.JoinDate.ToString("yyyy-MM-dd"),
        ["notes"] = m.Notes,
        ["rowVersion"] = m.RowVersion,
    };
}
