using Xunit;

namespace StockSim.IntegrationTests.Containers;

[CollectionDefinition(nameof(ContainersCollection))]
public sealed class ContainersCollection : ICollectionFixture<ContainersFixture> { }