using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Database.Authentication
{
    class AzureTokenAuthenticationManager : ISqlAuthenticationManager, IDisposable
    {
        private readonly ISqlTokenProvider tokenProvider;
        private readonly IConfiguration configuration;
        private readonly string connectionString;
        private readonly ConnectionMode connectionMode;
        private readonly Timer? tokenRefreshTimer;

        private string? accessToken = null;

        public AzureTokenAuthenticationManager(ISqlTokenProvider tokenProvider, IConfiguration configuration)
        {
            this.tokenProvider = tokenProvider;
            this.configuration = configuration;

            this.connectionString = configuration.GetConnectionString(nameof(ResourcesDbContext))
                                    ?? throw new InvalidOperationException($"Missing connection string for {nameof(ResourcesDbContext)}");

            if (!Enum.TryParse<ConnectionMode>(configuration["Database:ConnectionMode"], true, out ConnectionMode mode))
                mode = ConnectionMode.Default;

            this.connectionMode = mode;

            // Set a timer to refresh the token every 5 minutes, so the db connections always have a fresh token to use, and don't have to use sync-antipattern fetch.
            if (connectionMode == ConnectionMode.Tokens)
            {
                tokenRefreshTimer = new Timer(async (state) =>
                {
                    try { accessToken = await tokenProvider.GetAccessTokenAsync(); }
                    catch { /* Ignore */ }
                }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            }
        }

        public SqlConnection GetSqlConnection()
        {
            var connection = new SqlConnection(connectionString);

            if (connection.ConnectionString.Contains("database.windows.net") && connectionMode == ConnectionMode.Tokens)
            {
                if (accessToken == null)
                {
                    accessToken = AsyncUtils.RunSync(() => tokenProvider.GetAccessTokenAsync());
                }

                connection.AccessToken = accessToken;
            }

            return connection;
        }

        public void Dispose()
        {
            if (tokenRefreshTimer != null)
                tokenRefreshTimer.Dispose();
        }

        private enum ConnectionMode { Tokens, Default }
    }
}
