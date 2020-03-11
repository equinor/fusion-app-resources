using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LogicConfigExtensions
    {
        public static IServiceCollection AddResourceLogic(this IServiceCollection services)
        {
            services.AddMediatR(typeof(LogicConfigExtensions));

            return services;
        }
    }
}
