using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Resources.Application.LineOrg
{
    public static class LineOrgServiceExtensions
    {
        public static void UseLineOrgIntegration(this IServiceCollection services)
        {
            services
                .AddSingleton<ILineOrgResolver, LineOrgResolver>()
                .AddHostedService<LineOrgCacheRefresher>();
        }
    }
}
