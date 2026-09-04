namespace Shopizer.IntegrationTests.Fixtures;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ShopizerAspireCollection : ICollectionFixture<AspireHostFixture>
{
    public const string Name = "Shopizer Aspire";
}