using Fusion.AspNetCore.OData;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications.System
{
    public partial class OrgUnitDeleted
    {
        /// <summary>
        /// Handler to unassign allocation requests which has the deleted org unit as assigned department. 
        /// These requests should be returned to the unassigned pool so they can be claimed by new resource owners.
        /// </summary>
        public class UnassignAllocationRequestsHandler : INotificationHandler<OrgUnitDeleted>
        {
            private readonly ILogger<UnassignAllocationRequestsHandler> logger;
            private readonly IMediator mediator;

            public UnassignAllocationRequestsHandler(ILogger<UnassignAllocationRequestsHandler> logger, IMediator mediator)
            {
                this.logger = logger;
                this.mediator = mediator;
            }

            public async Task Handle(OrgUnitDeleted notification, CancellationToken cancellationToken)
            {
                // Fetch all requests that are assigned to the relevant department. 
                // Only fetch active requests.

                var query = new AspNetCore.OData.ODataQueryParams()
                {
                    Filter = ODataParser.Parse($"type eq '{InternalRequestType.Allocation}'"),

                    // Arbitrary number that should get most.. 
                    Top = 1000
                };

                var requests = await mediator.Send(new Queries.GetResourceAllocationRequests(query)
                    .ForAll()
                    .WithAssignedDepartment(notification.FullDepartment)
                    .WithExcludeCompleted());

                var exceptions = new List<Exception>();
                var failedRequestNumbers = new List<long>();

                // Need to set the editor scope as this is used to log editor in commands.
                using var systemAccountScope = mediator.SystemAccountScope();

                foreach (var request in requests)
                {
                    try
                    {
                        await mediator.Send(Commands.UpdateInternalRequest.UnassignRequest(request.RequestId));
                        logger.LogInformation("Unassigned '{AssignedDepartment}' from request {RequestNumber}", request.AssignedDepartment, request.RequestNumber);
                    }
                    catch (Exception ex)
                    {
                        failedRequestNumbers.Add(request.RequestNumber);
                        ex.Data.Add("requestNumber", $"{request.RequestNumber}");
                        exceptions.Add(ex);
                    }
                }

                if (exceptions.Any())
                    throw new AggregateException($"Failed to unassign {exceptions.Count} requests [{string.Join(",", failedRequestNumbers.Select(n => n.ToString()))}]", exceptions);
            }
        }
    }
}
