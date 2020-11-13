using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.TableStorage
{
    public static class CloudTableExtensions
    {
        public static async Task<T> GetByKeysAsync<T>(this CloudTable table, string partitionKey, string rowKey) where T : class, ITableEntity
        {
            var operation = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var result = await table.ExecuteAsync(operation);
            return result.Result as T;
        }
    }
}
