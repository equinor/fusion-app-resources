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

    private string _serviceBusConnectionString;
    private string _weeklySummaryQueueName;

    public DepartmentResourceOwnerSync(
        ILineOrgApiClient lineOrgApiClient, 
        ISummaryApiClient summaryApiClient,
        IConfiguration configuration)
    {
        this.lineOrgApiClient = lineOrgApiClient;
        this.summaryApiClient = summaryApiClient;
        this.configuration = configuration;
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
    [FunctionName("department-resource-owner-sync")]
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
        var departments = await lineOrgApiClient.GetOrgUnitDepartmentsAsync();

        var selectedDepartments = departments
            .Where(d => d.FullDepartment != null).DistinctBy(d => d.SapId).ToList();

        if (!selectedDepartments.Any())
            throw new Exception("No departments found.");

        // TODO: Retrieving resource-owners wil be refactored later to be more optimized
        // But this will do for the first iteration
        var resourceOwners = new List<LineOrgPerson>();
        foreach (var orgUnitsChunk in selectedDepartments.Chunk(10))
        {
            var chunkedResourceOwners =
                await lineOrgApiClient.GetResourceOwnersFromFullDepartment(orgUnitsChunk);
            resourceOwners.AddRange(chunkedResourceOwners);
        }

        if (!resourceOwners.Any())
            throw new Exception("No resource-owners found.");

        var resourceOwnerDepartments = resourceOwners
            .Where(ro => ro.DepartmentSapId is not null && Guid.TryParse(ro.AzureUniqueId, out _))
            .Select(resourceOwner => new
                ApiResourceOwnerDepartment(resourceOwner.DepartmentSapId!, resourceOwner.FullDepartment,
                    Guid.Parse(resourceOwner.AzureUniqueId)));


        var parallelOptions = new ParallelOptions()
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10,
        };

        // Use Parallel.ForEachAsync to easily limit the number of parallel requests
        await Parallel.ForEachAsync(resourceOwnerDepartments, parallelOptions,
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