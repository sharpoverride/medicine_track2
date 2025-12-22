using Xunit;

namespace MedicineTrack.End2EndTests.Fixtures;

/// <summary>
/// Collection definition that shares TestServicesFixture across all E2E tests.
/// This enables tests to run in both xUnit (Rider/VS) and the custom E2E Runner.
/// </summary>
[CollectionDefinition("E2ETests")]
public class E2ETestCollection : ICollectionFixture<TestServicesFixture>
{
    // This class has no code; it's used only to define the collection.
    // The collection fixture (TestServicesFixture) will be shared across all test classes
    // that use [Collection("E2ETests")] attribute.
}
