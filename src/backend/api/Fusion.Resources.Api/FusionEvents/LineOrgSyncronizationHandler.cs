using Fusion.Events;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Resources.Domain;
using Newtonsoft.Json;
using MediatR;
using System;
using System.Linq;

namespace Fusion.Resources.Api
{
    /// <summary>
    /// Handler to execute syncronisation events from the line org service. 
    /// We only want updates to be executed by a single instance in case of multiple instances running the api. 
    /// 
    /// The handler should accept events, collect relevant data, determine if event is relevant and dispatch events the domain should handle to perform updates.
    /// 
    /// Should consider what the handler actually does (running in context of the event execution) or if some work could be dispatched to an async handler (e.g. service bus). 
    /// If the handler throws an unhandled exception, the whole processing will be terminated and re-executed as the message is retried. If it consistently fail it will fall to a dead letter queue.
    /// 
    /// </summary>
    public class LineOrgSyncronizationHandler : ISubscriptionHandler
    {
        private readonly IMediator mediator;

        public LineOrgSyncronizationHandler(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task ProcessMessageAsync(MessageContext ctx, string? body, CancellationToken cancellationToken)
        {

            // Process the even 
            switch (ctx.Event.Type)
            {
                case var org when org == LineOrgEventTypes.OrgUnit.Name:
                    await HandleOrgUnitEventAsync(body);
                    break;
            }
        }

        private async Task HandleOrgUnitEventAsync(string? body)
        {
            var payloadData = JsonConvert.DeserializeObject<LineOrgEventBody>(body ?? "");

            if (payloadData is null)
            {
                return;
            }

            switch (payloadData.GetChangeType())
            {
                case LineOrgEventBody.ChangeType.Updated:
                    if (payloadData.Changes?.Any(i => i.EqualsIgnCase("fullDepartment")) == true) 
                    {
                        await HandleOrgUnitUpdatedAsync(payloadData);
                    }
                    break;

                case LineOrgEventBody.ChangeType.Deleted:
                    await HandleOrgUnitDeletedAsync(payloadData);
                    break;

            }
        }

        private async Task HandleOrgUnitUpdatedAsync(LineOrgEventBody payloadData)
        {            
            // Need to disable cache 
            using var disableCacheScope = new Fusion.Integration.DisableCacheScope();   // Need to verify this works..

            var orgUnit = await mediator.Send(new ResolveLineOrgUnit(payloadData.SapId));

            if (orgUnit is null) 
            {
                throw new InvalidOperationException("Updated org unit could no longer be resolved. Was it deleted as well?");
            }

            if (orgUnit.FullDepartment.EqualsIgnCase(payloadData.FullDepartment))
            {
                throw new InvalidOperationException("Resolved fullDepartment is same as existing. Caching issue? Terminating operation and hoping retry will solve issue.");
            }

            await mediator.Publish(new Domain.Notifications.System.OrgUnitPathUpdated(payloadData.SapId, payloadData.FullDepartment, orgUnit.FullDepartment));
        }

        private async Task HandleOrgUnitDeletedAsync(LineOrgEventBody payloadData)
        {
            // Need to disable cache 
            await mediator.Publish(new Domain.Notifications.System.OrgUnitDeleted(payloadData.SapId, payloadData.FullDepartment));
        }
    }
}