using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.ApiClients;
using Fusion.Resources.Functions.Functions.Notifications.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fusion.Resources.Functions.Functions.Notifications;

public class ScheduledReportContentBuilderFunction
{
    private readonly ILogger<ScheduledReportContentBuilderFunction> _logger;
    private readonly string _queueName;
    private readonly HttpClient _notificationsClient;

    public ScheduledReportContentBuilderFunction(ILogger<ScheduledReportContentBuilderFunction> logger,
        IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _queueName = configuration["scheduled_notification_report_queue"];
        _notificationsClient = httpClientFactory.CreateClient(HttpClientNames.Application.Notifications);
    }

    [FunctionName(ScheduledReportFunctionSettings.ContentBuilderFunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger("%scheduled_notification_report_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver)
    {
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' started with message: {message.Body}");
        try
        {
            var body = Encoding.UTF8.GetString(message.Body);
            var dto = JsonConvert.DeserializeObject<ScheduledNotificationQueueDto>(body);
            if (!Guid.TryParse(dto.AzureUniqueId, out var azureUniqueId))
                throw new Exception("AzureUniqueId not valid.");

            switch (dto.Role)
            {
                case NotificationRoleType.ResourceOwner:
                    await BuildContentForResourceOwner(azureUniqueId);
                    break;
                case NotificationRoleType.TaskOwner:
                    await BuildContentForTaskOwner(azureUniqueId);
                    break;
                default:
                    throw new Exception("Role not valid.");
            }

            // TODO: The message should be completed after the email has been sent.
            await messageReceiver.CompleteMessageAsync(message);

            _logger.LogInformation(
                $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' finished with message: {message.Body}");
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' failed with exception: {e.Message}");
        }
    }

    private async Task BuildContentForTaskOwner(Guid azureUniqueId)
    {
        throw new NotImplementedException();
    }

    private async Task BuildContentForResourceOwner(Guid azureUniqueId)
    {
        throw new NotImplementedException();
    }
}