using System;

namespace Fusion.Testing.Authentication
{
    public static class IntegrationTestingEnvSettings
    {
        public static bool IsIntegrationTest()
        {
            string environmentVariable = Environment.GetEnvironmentVariable(IntgTestEnvVariables.INTEGRATION_TEST_MARKER);
            return !string.IsNullOrEmpty(environmentVariable);
        }
    }
}
