using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Playbook.Application.Common.Abstractions;
using Playbook.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Playbook.Api.IntegrationTests.Uploads;

// Stub that records upload calls and returns predictable results.
internal sealed class StubBlobStore : IBlobStore
{
    public UploadedBlob? LastUpload { get; private set; }

    public Task<UploadedBlob> UploadAsync(string blobPath, Stream content, string contentType, string fileName, CancellationToken ct)
    {
        LastUpload = new UploadedBlob(blobPath, fileName, contentType, content.Length);
        return Task.FromResult(LastUpload);
    }

    public Task DeleteAsync(string blobPath, CancellationToken ct) => Task.CompletedTask;
    public Task<Uri> GetReadSasUrlAsync(string blobPath, TimeSpan ttl, CancellationToken ct) =>
        Task.FromResult(new Uri($"https://blob.example.com/{blobPath}?sas=test"));
    public Task<bool> ExistsAsync(string blobPath, CancellationToken ct) => Task.FromResult(true);
}

[Collection("Mongo")]
public sealed class UploadTests : IClassFixture<AppFactory>
{
    private readonly HttpClient _client;
    private readonly StubBlobStore _blobs = new();

    public UploadTests(AppFactory factory)
    {
        _client = factory.WithWebHostBuilder(b =>
            b.ConfigureServices(s =>
            {
                s.RemoveAll<IBlobStore>();
                s.AddSingleton<IBlobStore>(_blobs);
            })).CreateClient();
    }

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var email = $"uploader-{Guid.NewGuid():N}@test.com";
        var body = JsonSerializer.Serialize(new
        {
            query = $@"mutation {{ signup(input: {{ email: ""{email}"", password: ""Password1!"", displayName: ""Up"" }}) {{ accessToken }} }}"
        });
        var resp = await client.PostAsync("/graphql",
            new StringContent(body, Encoding.UTF8, "application/json"));
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("signup").GetProperty("accessToken").GetString()!;
    }

    private static MultipartFormDataContent JpegContent(long size = 512, string? activityId = null)
    {
        // Minimal JPEG magic bytes followed by padding
        var bytes = new byte[size];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;

        var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes) { Headers = { ContentType = new("image/jpeg") } }, "file", "test.jpg");
        if (activityId is not null)
            form.Add(new StringContent(activityId), "activityId");
        return form;
    }

    [Fact]
    public async Task Upload_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsync("/api/uploads", JpegContent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_ValidJpeg_Returns200WithBlobInfo()
    {
        var token = await GetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync("/api/uploads", JpegContent(activityId: "act123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("blobPath").GetString()));
        Assert.Equal("image/jpeg", body.GetProperty("contentType").GetString());
        Assert.Equal("test.jpg", body.GetProperty("fileName").GetString());
        Assert.True(body.GetProperty("sizeBytes").GetInt64() > 0);

        // Blob path should include the activityId segment
        Assert.Contains("act123", body.GetProperty("blobPath").GetString()!);
    }

    [Fact]
    public async Task Upload_NoActivityId_UsesPendingSegment()
    {
        var token = await GetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync("/api/uploads", JpegContent());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("pending", body.GetProperty("blobPath").GetString()!);
    }

    [Fact]
    public async Task Upload_DisallowedContentType_Returns400()
    {
        var token = await GetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var form = new MultipartFormDataContent();
        form.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes("<html></html>"))
            { Headers = { ContentType = new("text/html") } },
            "file", "evil.html");

        var response = await _client.PostAsync("/api/uploads", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_FakeImageMagicBytes_Returns400()
    {
        var token = await GetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Declare image/jpeg but send bytes that don't match any image signature
        var form = new MultipartFormDataContent();
        form.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes("this is not an image"))
            { Headers = { ContentType = new("image/jpeg") } },
            "file", "notreal.jpg");

        var response = await _client.PostAsync("/api/uploads", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_EmptyFile_Returns400()
    {
        var token = await GetTokenAsync(_client);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var form = new MultipartFormDataContent();
        form.Add(
            new ByteArrayContent(Array.Empty<byte>())
            { Headers = { ContentType = new("image/jpeg") } },
            "file", "empty.jpg");

        var response = await _client.PostAsync("/api/uploads", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
