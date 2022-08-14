using Xunit;

namespace SharpDetect.E2ETests.Definitions
{
    [CollectionDefinition("E2E")]
    public class E2ETestCollection : ICollectionFixture<CompilationFixture>
    {

    }
}
