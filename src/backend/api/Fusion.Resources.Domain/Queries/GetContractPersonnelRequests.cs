using Fusion.ApiClients.Org;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Org;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable
namespace Fusion.Resources.Domain.Queries
{


    public class GetContractPersonnelRequests : IRequest<IEnumerable<QueryPersonnelRequest>>
    {

        private GetContractPersonnelRequests(QueryType type)
        {
            Type = type;
        }

        public static GetContractPersonnelRequests QueryContract(Guid orgProjectId, Guid orgContractId) => new GetContractPersonnelRequests(QueryType.Contract)
        {
            OrgProjectId = orgProjectId,
            OrgContractId = orgContractId
        };


        public ODataQueryParams? Query { get; private set; }
        public Guid? OrgContractId { get; set; }
        public Guid? OrgProjectId { get; set; }

        public ExpandFields Expands { get; set; }

        private QueryType Type { get; set; }

        private enum QueryType { All, Project, Contract }

        [Flags]
        public enum ExpandFields 
        { 
            None                = 0,
            OriginalPosition    = 1 << 0,
            All                 = OriginalPosition
        }

        public GetContractPersonnelRequests WithQuery(ODataQueryParams query)
        {
            Query = query;

            if (query.ShoudExpand("originalPosition"))
                Expands |= ExpandFields.OriginalPosition;

            return this;
        }

        public class Handler : IRequestHandler<GetContractPersonnelRequests, IEnumerable<QueryPersonnelRequest>>
        {
            private readonly ResourcesDbContext resourcesDb;
            private readonly IProjectOrgResolver orgResolver;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext resourcesDb, IProjectOrgResolver orgResolver, IMediator mediator)
            {
                this.resourcesDb = resourcesDb;
                this.orgResolver = orgResolver;
                this.mediator = mediator;
            }

            public async Task<IEnumerable<QueryPersonnelRequest>> Handle(GetContractPersonnelRequests request, CancellationToken cancellationToken)
            {
                var query = request.Type switch
                {
                    QueryType.Contract => resourcesDb.ContractorRequests.Where(c => c.Project.OrgProjectId == request.OrgProjectId && c.Contract.OrgContractId == request.OrgContractId).AsQueryable(),
                    _ => throw new NotImplementedException("Query type not supported.")
                };


                if (request.Query != null && request.Query.HasFilter)
                {
                    query = query.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField("state", e => e.State);
                        m.MapField("category", e => e.Category);

                        m.MapField("createdBy.azureUniquePersonId", e => e.CreatedBy.AzureUniqueId);
                        m.MapField("createdBy.mail", e => e.CreatedBy.Mail);
                        m.MapField("created", e => e.Created);
                        m.MapField("updatedBy.azureUniquePersonId", e => e.CreatedBy.AzureUniqueId);
                        m.MapField("updatedBy.mail", e => e.CreatedBy.Mail);
                        m.MapField("updated", e => e.Created);

                        m.MapField("originalPositionId", e => e.OriginalPositionId);

                        m.MapField("position.workload", e => e.Position.Workload);
                        m.MapField("position.name", e => e.Position.Name);
                        m.MapField("position.appliesFrom", e => e.Position.AppliesFrom);
                        m.MapField("position.appliesTo", e => e.Position.AppliesTo);
                        m.MapField("position.basePositionId", e => e.Position.BasePositionId);

                        m.MapField("person.mail", e => e.Person.Person.Mail);
                        m.MapField("person.name", e => e.Person.Person.Name);
                        m.MapField("person.firstName", e => e.Person.Person.FirstName);
                        m.MapField("person.lastName", e => e.Person.Person.LastName);
                        m.MapField("person.azureAdStatus", e => e.Person.Person.AccountStatus);
                    });
                }


                var dbRequest = await query
                    .Include(r => r.Person).ThenInclude(p => p.Person).ThenInclude(p => p.Disciplines)
                    .Include(r => r.Person).ThenInclude(p => p.CreatedBy)
                    .Include(r => r.Person).ThenInclude(p => p.UpdatedBy)
                    .Include(r => r.Person).ThenInclude(p => p.Project)
                    .Include(r => r.Person).ThenInclude(p => p.Contract)
                    .Include(r => r.CreatedBy)
                    .Include(r => r.UpdatedBy)
                    .Include(r => r.Project)
                    .Include(r => r.Contract)
                    .OrderByDescending(r => r.LastActivity)
                    .ToListAsync();

                var basePositions = await Task.WhenAll(dbRequest
                    .Select(q => q.Position.BasePositionId)
                    .Distinct()
                    .Select(bp => orgResolver.ResolveBasePositionAsync(bp))
                );

                var resolvedOrgChartPositions = request.Expands.HasFlag(ExpandFields.OriginalPosition) ?
                    await orgResolver.ResolvePositionsAsync(dbRequest.Where(r => r.OriginalPositionId.HasValue).Select(r => r.OriginalPositionId!.Value)) :
                    new List<ApiPositionV2>();

                var workflows = await mediator.Send(new GetRequestWorkflows(dbRequest.Select(r => r.Id)));

                var positions = dbRequest.Select(p =>
                {
                    var position = new QueryPositionRequest(p.Position)
                        .WithResolvedBasePosition(basePositions.FirstOrDefault(bp => bp!.Id == p.Position.BasePositionId));
                    var workflow = workflows.FirstOrDefault(wf => wf.RequestId == p.Id);

                    var personnelRequest = new QueryPersonnelRequest(p, position, workflow);

                    // Expand original position.
                    if (personnelRequest.OriginalPositionId.HasValue)
                    {
                        var originalPosition = resolvedOrgChartPositions.FirstOrDefault(p => p.Id == personnelRequest.OriginalPositionId.Value);
                        if (originalPosition != null)
                            personnelRequest.WithResolvedOriginalPosition(originalPosition);
                    }

                    return personnelRequest;
                }).ToList();

                return positions;
            }
        }


    }
}

