using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace CharityCrm.Api.IntegrationTests.Infrastructure;

/// <summary>HttpClient wrapper that behaves like the SPA: cookies plus the X-XSRF-TOKEN header (NFR-SEC-02).</summary>
public sealed class ApiClient(HttpClient http)
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private string? _xsrfToken;

    public HttpClient Http => http;

    public Task<HttpResponseMessage> GetAsync(string url) => http.GetAsync(url);

    public async Task<T> GetJsonAsync<T>(string url)
    {
        var response = await http.GetAsync(url);
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public Task<HttpResponseMessage> PostAsync(string url, object? body = null) => SendAsync(HttpMethod.Post, url, body);

    public Task<HttpResponseMessage> PutAsync(string url, object? body) => SendAsync(HttpMethod.Put, url, body);

    public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body)
    {
        _xsrfToken ??= (await http.GetFromJsonAsync<TokenBody>("/api/auth/antiforgery"))!.Token;

        using var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body ?? new { }, options: Json) };
        request.Headers.Add("X-XSRF-TOKEN", _xsrfToken);
        var response = await http.SendAsync(request);

        // Tokens are bound to the signed-in identity; fetch a new one after it changes.
        if (url.StartsWith("/api/auth/", StringComparison.Ordinal))
        {
            _xsrfToken = null;
        }
        return response;
    }

    public async Task<T> PostJsonAsync<T>(string url, object? body, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var response = await PostAsync(url, body);
        await response.ShouldHaveStatusAsync(expected);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task<T> PutJsonAsync<T>(string url, object? body)
    {
        var response = await PutAsync(url, body);
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task LoginAsync(string email, string password)
    {
        var response = await PostAsync("/api/auth/login", new { email, password });
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    private sealed record TokenBody(string Token);
}

public static class HttpResponseAssertions
{
    public static async Task ShouldHaveStatusAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(expected, "response body was: {0}", body);
        }
    }

    public static async Task<Problem> ReadProblemAsync(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<Problem>(ApiClient.Json))!;
}

public sealed record Problem(int Status, string? Title, string? Detail, string? Code, Dictionary<string, string[]>? Errors);
