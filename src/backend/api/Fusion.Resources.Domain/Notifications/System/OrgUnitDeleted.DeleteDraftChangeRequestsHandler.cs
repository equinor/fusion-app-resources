using Fusion.AspNetCore.OData;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Notifications.System
{
    public partial class OrgUnitDeleted
    {
        /// <summary>
        /// If an org unit has created draft org units, we should remove these as they will never be started when the org unit has been deleted.
        /// For those that has actually be sent, we should not unassign as they can still be accepted by the task. 
        /// </summary>
        public class DeleteDraftChangeRequestsHandler : INotificationHandler<OrgUnitDeleted>
        {
            private readonly ILogger<DeleteDraftChangeRequestsHandler> logger;
            private readonly IMediator mediator;

            public DeleteDraftChangeRequestsHandler(ILogger<DeleteDraftChangeRequestsHandler> logger, IMediator mediator)
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
                    Filter = ODataParser.Parse("isDraft eq 'true'"),

                    // Arbitrary number that should get most.. 
                    Top = 1000
                };

                var requests = await mediator.Send(new Queries.GetResourceAllocationRequests(query)
                    .ForResourceOwners()
                    .WithAssignedDepartment(notification.FullDepartment)
                    .WithExcludeCompleted());

                using var systemAccountScope = mediator.SystemAccountScope();

                foreach (var request in requests)
                {
                    if (request.IsDraft)
                    {
                        // Delete
                        await mediator.Send(new Commands.DeleteInternalRequest(request.RequestId));
                        logger.LogInformation("Deleted draft change request {RequestNumber} in '{AssignedDepartment}'", request.RequestNumber, request.AssignedDepartment);

                    }
                }
            }
        }
    }
}
