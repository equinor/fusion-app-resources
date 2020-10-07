
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Fusion.Testing
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection TryRemoveImplementationService<TService>(this IServiceCollection services)
        {
            var descriptor = services.FirstOrDefault(sd => sd.ImplementationType == typeof(TService));
            if (descriptor != null)
                services.Remove(descriptor);

            return services;
        }

        public static IServiceCollection TryRemoveImplementationService(this IServiceCollection services, string typeName)
        {
            var descriptor = services.FirstOrDefault(sd => sd.ImplementationType?.Name == typeName);
            if (descriptor != null)
                services.Remove(descriptor);

            return services;
        }

        public static IServiceCollection TryRemoveTransientEventHandlers(this IServiceCollection services)
        {

            var hostedServices = services
                .Where(sd => sd.ServiceType == typeof(IHostedService) && sd.ImplementationFactory != null)
                .Where(sd => sd.ImplementationFactory.Method.Name.Contains("AddFusionEventHandler"))
                .ToList();

            hostedServices.ForEach(s => services.Remove(s));

            services.TryRemoveImplementationService("EventHandlerFactory");

            return services;
        }

        public static IServiceCollection AddSingletonIfFound<T, TType>(this IServiceCollection services)
            where T : class
            where TType : class, T
        {
            var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(T));
            if (descriptor == null)
                return services;

            services.AddSingleton<T, TType>();
            return services;
        }
    }
}
