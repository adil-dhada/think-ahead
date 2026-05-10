using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Playbook.Infrastructure.Mongo;
using Xunit;

namespace Playbook.Api.IntegrationTests.Fixtures;

public sealed class AppFactory(MongoFixture mongo) : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = mongo.ConnectionString,
                ["Mongo:Database"] = $"playbook-test-{Guid.NewGuid():N}",
                ["Jwt:SigningKey"] = "integration-test-signing-key-min-32-bytes!",
                ["Jwt:Issuer"] = "playbook-api",
                ["Jwt:Audience"] = "playbook-web",
                ["Blob:ConnectionString"] = "UseDevelopmentStorage=true",
                ["Blob:Container"] = "pb-files-test"
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public new Task DisposeAsync() => base.DisposeAsync().AsTask();
}
