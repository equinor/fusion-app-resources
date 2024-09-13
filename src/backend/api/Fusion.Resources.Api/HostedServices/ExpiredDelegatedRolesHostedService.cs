using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Api.HostedServices
{
    public class ExpiredDelegatedRolesHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;
        private Timer? timer;

        public ExpiredDelegatedRolesHostedService(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public override void Dispose()
        {
            timer?.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Support multiple server instances, but randomizing the execution. 
            var random = new Random(DateTime.Now.GetHashCode());

            var minutesToHour = 60 - DateTime.UtcNow.Minute;
            var delayInterval = minutesToHour > 3 ? TimeSpan.FromMinutes(random.Next(minutesToHour - 3, minutesToHour + 3)) : TimeSpan.FromMinutes(random.Next(0, 5));

            timer = new Timer(async _ => await ExecuteTimerAsync(), null, delayInterval, TimeSpan.FromMinutes(60));

            return Task.CompletedTask;
        }

        internal async Task ExecuteTimerAsync()
        {
            using var serviceScope = serviceScopeFactory.CreateScope();
            var telemetryClient = serviceScope.ServiceProvider.GetRequiredService<TelemetryClient>();
            var dbContext = serviceScope.ServiceProvider.GetRequiredService<ResourcesDbContext>();

            var operation = telemetryClient.StartOperation<RequestTelemetry>("Checking expired delegated department responsibles");
            operation.Telemetry.Source = "Fusion.Resource.Api service";

            try
            {
                await CheckDelegatedDepartmentResponsibleExpirationAsync(telemetryClient, dbContext);
                operation.Telemetry.Success = true;
            }
            catch (Exception ex)
            {
                telemetryClient.TrackTrace("Error executing the expiration check");
                telemetryClient.TrackException(ex);

                operation.Telemetry.Success = false;
            }
            finally
            {
                telemetryClient.StopOperation(operation);
            }
        }

        private static async Task CheckDelegatedDepartmentResponsibleExpirationAsync(TelemetryClient telemetryClient, ResourcesDbContext db)
        {
            var toExpire = await db.DelegatedDepartmentResponsibles
                .Where(r => r.DateTo <= DateTime.UtcNow)
                .ToListAsync();

            telemetryClient.TrackTrace($"Found {toExpire.Count} delegated responsible roles that will expire");
            telemetryClient.TrackMetric(new MetricTelemetry("expiredDelegatedDepartmentResponsibles", toExpire.Count));

            toExpire.ForEach(r => telemetryClient.TrackTrace($"Moving expired delegated responsibility for department '{r.DepartmentId}' " +
                                                                  $"for person with azureUniqueId '{r.ResponsibleAzureObjectId}' to the history table", SeverityLevel.Information,
                new Dictionary<string, string> {
                    { "dateFrom", $"{r.DateFrom}" },
                    { "dateTo", $"{r.DateTo}" },
                    { "updatedByAzureUniqueId", $"{r.UpdatedBy}" },
                    { "reason", $"{r.Reason}" }
                }));

            db.DelegatedDepartmentResponsibles.RemoveRange(toExpire);

            var expiredRecords = toExpire.Select(x => new DbDelegatedDepartmentResponsibleHistory(x));
            await db.DelegatedDepartmentResponsiblesHistory.AddRangeAsync(expiredRecords);
            await db.SaveChangesAsync();
        }
    }
}
