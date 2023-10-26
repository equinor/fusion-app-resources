using System;
using System.Threading.Tasks;
using Fusion.Resources.Functions.ApiClients.ApiModels;

namespace Fusion.Resources.Functions.ApiClients;

public interface INotificationApiClient
{
    Task<bool> SendNotification(SendNotificationsRequest request, Guid azureUniqueId);
}