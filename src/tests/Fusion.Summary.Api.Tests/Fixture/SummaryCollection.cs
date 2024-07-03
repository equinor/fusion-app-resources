namespace Fusion.Summary.Api.Tests.Fixture;

[CollectionDefinition(TestCollections.SUMMARY)]
public class SummaryCollection : ICollectionFixture<SummaryApiFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public static class TestCollections
{
    public const string SUMMARY = "SummaryTests";
}