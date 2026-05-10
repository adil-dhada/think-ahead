using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Playbook.Api.IntegrationTests.Fixtures;

public sealed class MongoFixture : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public IMongoDatabase GetDatabase(string name = "playbook-test")
    {
        var client = new MongoClient(ConnectionString);
        return client.GetDatabase(name);
    }

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
