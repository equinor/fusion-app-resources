using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.TableStorage
{
    public class TableStorageClient
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<TableStorageClient> logger;

        public TableStorageClient(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.logger = loggerFactory.CreateLogger<TableStorageClient>();
        }

        public async Task<CloudTable> GetTableAsync(string tableName)
        {
            string storageConnectionString = configuration.GetValue<string>("AzureWebJobsStorage");

            var storageAccount = CreateStorageAccountFromConnectionString(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference(tableName);

            if (await table.CreateIfNotExistsAsync())
            {
                logger.LogInformation($"Created Table named: {tableName}");
            }
            return table;
        }

        private CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                logger.LogError("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app configuration");
                throw;
            }
            catch (ArgumentException)
            {
                logger.LogError("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app configuration");
                throw;
            }

            return storageAccount;
        }
    }
}
