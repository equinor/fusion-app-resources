using System;
using System.Collections.Generic;
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

    public ScheduledReportContentBuilderFunction(ILogger<ScheduledReportContentBuilderFunction> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _queueName = configuration["scheduled_notification_report_queue"];
    }

    [FunctionName(ScheduledReportFunctionSettings.ContentBuilderFunctionName)]
    public async Task RunAsync(
        [ServiceBusTrigger("%scheduled_notification_report_queue%", Connection = "AzureWebJobsServiceBus")]
        ServiceBusReceivedMessage message, ServiceBusMessageActions messageReceiver)
    {
        var body = Encoding.UTF8.GetString(message.Body);
        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' started with message: {body}");
        var dto = JsonConvert.DeserializeObject<ScheduledNotificationQueueDto>(body);
        if (!Guid.TryParse(dto.AzureUniqueId, out var azureId))
        {
            _logger.LogError(
                $"ServiceBus queue '{_queueName}', error receiving message: azureId not valid");
            return;
        }

        switch (dto.Role)
        {
            case NotificationRoleType.ResourceOwner:
                await BuildContentForResourceOwner(azureId);
                break;
            case NotificationRoleType.TaskOwner:
                await BuildContentForTaskOwner(azureId);
                break;
            default:
                _logger.LogError(
                    $"ServiceBus queue '{_queueName}', error receiving message: role not valid");
                return;
        }

        // TODO: The message should be completed after the email has been sent.
        await messageReceiver.CompleteMessageAsync(message);

        _logger.LogInformation(
            $"Function '{ScheduledReportFunctionSettings.ContentBuilderFunctionName}' finished with message: {message.Body}");
    }

    private async Task BuildContentForTaskOwner(Guid azureId)
    {
        throw new NotImplementedException();
    }

    private async Task BuildContentForResourceOwner(Guid azureId)
    {
        throw new NotImplementedException();
    }
}