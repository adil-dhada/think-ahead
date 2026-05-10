using Xunit;

namespace Playbook.Api.IntegrationTests.Fixtures;

[CollectionDefinition("Mongo")]
public sealed class MongoCollection : ICollectionFixture<MongoFixture>;
