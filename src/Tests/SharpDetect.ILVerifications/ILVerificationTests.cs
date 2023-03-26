// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.TestUtils;
using SharpDetect.TestUtils.E2E;
using SharpDetect.TestUtils.Verifications;
using Xunit;

namespace SharpDetect.ILVerifications
{
    [Collection("E2E")]
    public class ILVerificationTests
    {
        [Theory]
        [InlineData(nameof(E2ETests.Subject.Program.Verify_ArrayElementAccess_Instrumentation))]
        [InlineData(nameof(E2ETests.Subject.Program.Verify_FieldElementAccess_Instrumentation))]
        public async Task ILVerificationTests_AllMethodAndTypesVerified(string testName)
        {
            // Prepare
            var analysisTarget = Path.GetFileName(E2ETestsConfiguration.SubjectDllPath);
            await PublishHelpers.PublishTestAsync(E2ETestsConfiguration.SubjectCsprojPath, E2ETestsConfiguration.ILVerificationPath);
            await using var session = SessionHelpers.CreateAnalysisSession(
                executablePath: E2ETestsConfiguration.SubjectDllPath,
                plugins: "Nop", 
                args: testName,
                additionalConfiguration: VerificationUtils.GetConfigurationAdditions());

            // Act
            await session.Start();

            // Assert
            await VerificationUtils.AssertAllMethodsAndTypesVerified(analysisTarget, E2ETestsConfiguration.ILVerificationPath);
        }
    }
}
