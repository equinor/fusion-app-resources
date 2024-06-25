using Fusion.Resources.Functions.Common.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.Common.ApiClients;

public interface INotificationApiClient
{
    Task<bool> SendNotification(SendNotificationsRequest request, Guid azureUniqueId);
}