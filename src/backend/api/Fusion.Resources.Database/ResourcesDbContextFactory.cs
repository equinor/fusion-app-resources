using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Fusion.Resources.Database
{
    public class ResourcesDbContextFactory : IDesignTimeDbContextFactory<ResourcesDbContext>
    {
        public ResourcesDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("Using designtime factory...");

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddUserSecrets("474454c7-2021-4f46-bfd4-02b221fc3fa0"); // From the api project
            
            IConfiguration config = configurationBuilder.Build();

            /*
             * The designtime factory will override the connection strings used in api project.
             * To point to an actual database in dev, update the usersecret in Fusion.Resources.Api.
             * 
             * */

            var optionsBuilder = new DbContextOptionsBuilder<ResourcesDbContext>();


            var connectionString = config.GetConnectionString(nameof(ResourcesDbContext));
            if (!string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Using connection string: " + connectionString);

                if (config["EF:Design:UseAccessToken"] == "true")
                {
                    SqlConnection conn = new SqlConnection(connectionString);

                    try
                    {
                        var msiConnectionString = config.GetConnectionString(nameof(AzureServiceTokenProvider));
                        conn.AccessToken = (new AzureServiceTokenProvider(msiConnectionString)).GetAccessTokenAsync("https://database.windows.net/").Result;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed getting access token for signed in user. Refer documention to specify app service token connection string.");
                        Console.WriteLine("Error message: " + ex.Message);
                        Console.WriteLine("https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication#connection-string-support");
                        throw;
                    }

                    optionsBuilder.UseSqlServer(conn);
                }
                else
                {
                    optionsBuilder.UseSqlServer(connectionString);
                }
            }
            else
            {
                optionsBuilder.UseSqlServer("Data Source=blog.db");
            }

            return new ResourcesDbContext(optionsBuilder.Options);
        }
    }
}
