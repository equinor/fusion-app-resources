using Microsoft.AspNetCore.Authentication;

namespace Fusion.Testing.Authentication.Configuration
{

    public static class AuthenticationSchemeOptionsExtensions
    {
        /// <summary>
        /// Enable authentication forwards. Debug build is required for option to have any effect, 
        /// and the environment integration test marker must be set ("INTEGRATION_TEST_RUN")
        /// </summary>
        /// <param name="options"></param>
        public static void EnableIntegrationTestForward(this AuthenticationSchemeOptions options)
        {
             options.ForwardAuthenticate = IntegrationTestAuthDefaults.AuthenticationScheme;
        }


    }
}
