using System;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.ApiClients
{
    public interface INotificationApiClient
    {
        Task<int?> GetDelayForUserAsync(Guid azureUniqueId);

        Task<bool> PostNewNotificationAsync(Guid recipientAzureId, string title, string bodyMarkdown);


    }
}