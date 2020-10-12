using Fusion.Testing.Authentication;
using Fusion.Testing.Authentication.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class FusionAuthenticationBuilderExtensions
    {
        /// <summary>
        /// Adds the integration test authentication handler. 
        /// This will only be added if the EnvSettings.IsIntegrationTesting variable is set.
        /// </summary>
        /// <returns></returns>
        //public static FusionAuthenticationBuilder AddIntegrationTestingAuthentication(this FusionAuthenticationBuilder builder)
        //{
        //    builder.authBuilder.AddScheme<IntegrationTestAuthOptions, IntegrationTestAuthHandler>(IntegrationTestAuthDefaults.AuthenticationScheme, opts => { });

        //    return builder;
        //}

        public static IServiceCollection AddIntegrationTestingAuthentication(this IServiceCollection services)
        {
            var builder = services.AddAuthentication();

            builder.AddScheme<IntegrationTestAuthOptions, IntegrationTestAuthHandler>(IntegrationTestAuthDefaults.AuthenticationScheme, opts => { });

            if (Environment.GetEnvironmentVariable("FORWARD_JWT") != null)
                services.PostConfigureAll<JwtBearerOptions>(o => o.ForwardAuthenticate = IntegrationTestAuthDefaults.AuthenticationScheme);

            if (Environment.GetEnvironmentVariable("FORWARD_COOKIE") != null)
                services.PostConfigureAll<CookieAuthenticationOptions>(o => o.ForwardAuthenticate = IntegrationTestAuthDefaults.AuthenticationScheme);

            return services;
        }
    }

    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// This option will turn on forwarding of authentication for the JWT bearer layer to the integration test handler. 
        /// 
        /// The forward will ONLY occur when the application is built with DEBUG configuration and the integration marker is set in environment variables.
        /// </summary>
        public static IServiceCollection InterceptBearerAuthentication(this IServiceCollection services)
        {

            services.Configure<JwtBearerOptions>(options =>
            {
                options.EnableIntegrationTestForward();
            });

            return services;
        }
    }

}
