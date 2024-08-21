using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Resources.Functions.Common.ApiClients.ApiModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Fusion.Summary.Functions.Functions;

public class DepartmentResourceOwnerSync
{
    private readonly ILineOrgApiClient lineOrgApiClient;
    private readonly ISummaryApiClient summaryApiClient;
    private readonly IConfiguration configuration;
    private readonly IResourcesApiClient resourcesApiClient;

    private string _serviceBusConnectionString;
    private string _weeklySummaryQueueName;

    public DepartmentResourceOwnerSync(
        ILineOrgApiClient lineOrgApiClient, 
        ISummaryApiClient summaryApiClient,
        IConfiguration configuration,
        IResourcesApiClient resourcesApiClient)
    {
        this.lineOrgApiClient = lineOrgApiClient;
        this.summaryApiClient = summaryApiClient;
        this.configuration = configuration;
        this.resourcesApiClient = resourcesApiClient;
    }

    /// <summary>
    /// Function does two things:
    /// - Fetches all departments and updates the database
    /// - Sends the department info to the weekly summary queue for the workers to pick up
    /// </summary>
    /// <param name="timerInfo">The running date & time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    [FunctionName("weekly-department-recipients-sync")]
    public async Task RunAsync(
        [TimerTrigger("0 05 00 * * *", RunOnStartup = false)]
        TimerInfo timerInfo, CancellationToken cancellationToken
    )
    {
        _serviceBusConnectionString = configuration["AzureWebJobsServiceBus"];
        _weeklySummaryQueueName = configuration["department_summary_weekly_queue"];

        var client = new ServiceBusClient(_serviceBusConnectionString);
        var sender = client.CreateSender(_weeklySummaryQueueName);

        // Fetch all departments
        var departments = (await lineOrgApiClient.GetOrgUnitDepartmentsAsync())
            .DistinctBy(d => d.SapId)
            .Where(d => d.FullDepartment != null && d.SapId != null)
            .Where(d => d.FullDepartment!.Contains("PRD"))
            .Where(d => d.Management.Persons.Length > 0);


        var apiDepartments = new List<ApiResourceOwnerDepartment>();

        foreach (var orgUnit in departments)
        {
            var delegatedResponsibles = (await resourcesApiClient
                    .GetDelegatedResponsibleForDepartment(orgUnit.SapId!))
                .Select(d => Guid.Parse(d.DelegatedResponsible.AzureUniquePersonId))
                .Distinct()
                .ToArray();

            apiDepartments.Add(new ApiResourceOwnerDepartment()
            {
                DepartmentSapId = orgUnit.SapId!,
                FullDepartmentName = orgUnit.FullDepartment!,
                ResourceOwnersAzureUniqueId = orgUnit.Management.Persons.Select(p => Guid.Parse(p.AzureUniqueId)).Distinct().ToArray(),
                DelegateResourceOwnersAzureUniqueId = delegatedResponsibles
            });
        }

        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10,
        };

        // Use Parallel.ForEachAsync to easily limit the number of parallel requests
        await Parallel.ForEachAsync(apiDepartments, parallelOptions,
            async (ownerDepartment, token) =>
            {
                // Update the database
                await summaryApiClient.PutDepartmentAsync(ownerDepartment, token);

                // Send queue message
                await SendDepartmentToQueue(sender, ownerDepartment);
            });
    }

    private async Task SendDepartmentToQueue(ServiceBusSender sender, ApiResourceOwnerDepartment department, double delayInMinutes = 0)
    {
        var serializedDto = JsonConvert.SerializeObject(department);

        var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedDto))
        {
            ScheduledEnqueueTime = DateTime.UtcNow.AddMinutes(delayInMinutes)
        };

        await sender.SendMessageAsync(message);
    }
}