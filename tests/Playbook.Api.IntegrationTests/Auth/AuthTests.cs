using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Playbook.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Playbook.Api.IntegrationTests.Auth;

[Collection("Mongo")]
public sealed class AuthTests(AppFactory factory) : IClassFixture<AppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static StringContent GraphQL(string query, object? variables = null) =>
        new(JsonSerializer.Serialize(new { query, variables }),
            System.Text.Encoding.UTF8, "application/json");

    [Fact]
    public async Task Signup_Returns_AccessToken()
    {
        var response = await _client.PostAsync("/graphql", GraphQL(@"
            mutation {
              signup(input: { email: ""test@example.com"", password: ""password123"", displayName: ""Tester"" }) {
                accessToken
                user { email displayName }
              }
            }"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("data").GetProperty("signup").GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task Login_WithBadCredentials_ReturnsForbiddenError()
    {
        var response = await _client.PostAsync("/graphql", GraphQL(@"
            mutation {
              login(input: { email: ""nobody@example.com"", password: ""wrong"" }) {
                accessToken
              }
            }"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out _), "Expected errors in response");
    }
}
