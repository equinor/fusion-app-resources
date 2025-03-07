using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Fusion.Resources.Database.Authentication;
using Fusion.Resources.Database;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseConfigurationExtensions
    {
        public static IServiceCollection AddResourceDatabase<TTokenProvider>(this IServiceCollection services, IConfiguration configuration)
            where TTokenProvider : class, ISqlTokenProvider
        {
            string migrationAssemblyName = Assembly.GetExecutingAssembly().FullName!;

            services.AddDbContext<ResourcesDbContext>(options => {
                // Connection string is handled by auth manager.
				options.UseSqlServer(setup => setup.MigrationsAssembly(migrationAssemblyName));
			});

            services.AddSingleton<ISqlAuthenticationManager, AzureTokenAuthenticationManager>();
            services.AddScoped<ITransactionScope, EFTransactionScope>();
            services.AddSingleton<ISqlTokenProvider, TTokenProvider>();

            return services;
        }
    }
}
