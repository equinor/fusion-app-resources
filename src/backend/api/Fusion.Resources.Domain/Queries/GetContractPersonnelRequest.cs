using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetContractPersonnelRequest : IRequest<QueryPersonnelRequest>
    {
        public GetContractPersonnelRequest(Guid requestId)
        {
            RequestId = requestId;
        }

        public Guid RequestId { get; }

        public ODataQueryParams? Query { get; private set; }

        public GetContractPersonnelRequest WithQuery(ODataQueryParams query)
        {
            if (query.ShoudExpand("comments"))
            {
                Expands |= ExpandProperties.RequestComments;
            }

            Query = query;

            return this;
        }

        public ExpandProperties Expands { get; set; }

        [Flags]
        public enum ExpandProperties
        {
            None = 0,
            RequestComments = 1 << 0,
            All = RequestComments
        }

        public class Handler : IRequestHandler<GetContractPersonnelRequest, QueryPersonnelRequest>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMediator mediator;
            private readonly TelemetryClient telemetryClient;

            public Handler(ResourcesDbContext resourcesDb, IProjectOrgResolver orgResolver, IMediator mediator, TelemetryClient telemetryClient)
            {
                this.resourcesDb = resourcesDb;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
                this.telemetryClient = telemetryClient;
            }

            public async Task<QueryPersonnelRequest> Handle(GetContractPersonnelRequest request, CancellationToken cancellationToken)
            {
                var dbRequest = await resourcesDb.ContractorRequests
                    .Include(r => r.Person).ThenInclude(p => p.Person).ThenInclude(p => p.Disciplines)
                    .Include(r => r.Person).ThenInclude(p => p.CreatedBy)
                    .Include(r => r.Person).ThenInclude(p => p.UpdatedBy)
                    .Include(r => r.Person).ThenInclude(p => p.Project)
                    .Include(r => r.Person).ThenInclude(p => p.Contract)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.Contract)
                    .FirstOrDefaultAsync(r => r.Id == request.RequestId);

                if (dbRequest is null)
                    throw new RequestNotFoundError(request.RequestId);

                var basePosition = await orgResolver.ResolveBasePositionAsync(dbRequest.Position!.BasePositionId);

                var position = new QueryPositionRequest(dbRequest.Position)
                    .WithResolvedBasePosition(basePosition);

                var workflow = await mediator.Send(new GetRequestWorkflow(request.RequestId));

                var returnItem = new QueryPersonnelRequest(dbRequest, position, workflow);

                if (request.Expands.HasFlag(ExpandProperties.RequestComments))
                {
                    var comments = await mediator.Send(new GetRequestComments(request.RequestId));
                    returnItem.WithComments(comments);
                }

                await TryResolveOriginalPositionAsync(returnItem);

                return returnItem;
            }

            private async Task TryResolveOriginalPositionAsync(QueryPersonnelRequest request)
            {
                if (request.OriginalPositionId.HasValue)
                {
                    try
                    {
                        var originalPosition = await orgResolver.ResolvePositionAsync(request.OriginalPositionId.Value);

                        if (originalPosition is null)
                            throw new Exception($"Could locate any position id '{originalPosition}'");

                        request.WithResolvedOriginalPosition(originalPosition);

                    }
                    catch (Exception ex)
                    {
                        // TODO: Log error
                        telemetryClient.TrackException(ex);
                        telemetryClient.TrackTrace($"Could not resolve original position on request {request.Id}: {ex.Message}");
                    }

                }
            }
        }
    }
}

