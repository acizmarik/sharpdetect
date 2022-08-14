using SharpDetect.E2ETests.Utilities;

namespace SharpDetect.E2ETests.Definitions
{
    public class CompilationFixture
    {
        public CompilationFixture()
        {
            Task.Run(() => CompilationHelpers.CompileTestAsync(TestsConfiguration.SubjectCsprojPath)).Wait();
        }
    }
}
