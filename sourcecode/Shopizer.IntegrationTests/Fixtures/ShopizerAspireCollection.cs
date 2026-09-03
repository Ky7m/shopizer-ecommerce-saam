namespace Shopizer.IntegrationTests.Fixtures;

[CollectionDefinition(Name, DisableParallelization = false)]
public sealed class ShopizerAspireCollection : ICollectionFixture<AspireHostFixture>
{
    public const string Name = "Shopizer Aspire";
}