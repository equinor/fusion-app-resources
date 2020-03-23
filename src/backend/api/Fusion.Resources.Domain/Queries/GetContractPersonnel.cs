using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.AspNetCore.OData;
using Fusion.Integration;
using Fusion.Integration.Profile;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain
{
    public class GetContractPersonnel : IRequest<IEnumerable<QueryContractPersonnel>>
    {
        private ODataQueryParams? query = null;

        public GetContractPersonnel(Guid contractId, ODataQueryParams? query = null)
        {
            ContractId = contractId;
            Query = query;

            
        }

        public Guid ContractId { get; set; }
        public ODataQueryParams? Query { get => query; set
            {
                this.query = value;

                if (value != null)
                {
                    if (value.ShoudExpand("requests"))
                        Expands |= ExpandProperties.Requests;

                    if (value.ShoudExpand("positions"))
                        Expands |= ExpandProperties.Positions;
                }
            }
        }


        public ExpandProperties Expands { get; set; }

        [Flags]
        public enum ExpandProperties {
            None        = 0,
            Positions   = 1 << 0,
            Requests    = 1 << 1,

            All = Positions | Requests
        }


        public class Handler : IRequestHandler<GetContractPersonnel, IEnumerable<QueryContractPersonnel>>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(ResourcesDbContext db, IFusionProfileResolver profileResolver, IProjectOrgResolver orgResolver)
            {
                this.db = db;
                this.profileResolver = profileResolver;
                this.orgResolver = orgResolver;
            }

            public async Task<IEnumerable<QueryContractPersonnel>> Handle(GetContractPersonnel request, CancellationToken cancellationToken)
            {
                var itemQuery = db.ContractPersonnel.Where(p => p.Contract.OrgContractId == request.ContractId)                    
                    .AsQueryable();

                if (request.Query != null && request.Query.HasFilter)
                    itemQuery = itemQuery.ApplyODataFilters(request.Query, m =>
                    {
                        m.MapField("azureAdStatus", p => p.Person.AccountStatus);
                        m.MapField("name", p => p.Person.Name);
                        m.MapField("phoneNumber", p => p.Person.Phone);
                        m.MapField("created", p => p.Created);
                    });

                var items = await itemQuery
                    .Include(i => i.Contract)
                    .Include(i => i.Project)
                    .Include(i => i.UpdatedBy)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Person).ThenInclude(p => p.Disciplines)
                    .ToListAsync();


                var returnItems = items.Select(i => new QueryContractPersonnel(i))
                    .ToList();


                if (request.Expands.HasFlag(ExpandProperties.Requests))
                {
                    await ExpandRequestsAsync(returnItems);
                }

                if (request.Expands.HasFlag(ExpandProperties.Positions))
                {
                    await ExpandPositionsAsync(returnItems);
                }

                return returnItems;
            }

            private async Task ExpandPositionsAsync(IEnumerable<QueryContractPersonnel> personnelItems)
            {
                var azureIds = personnelItems.Where(i => i.AzureUniqueId.HasValue).Select(i => i.AzureUniqueId!.Value);

                var profiles = new List<FusionFullPersonProfile>();

                int index = 0;
                while (true)
                {
                    var page = azureIds.Skip(index).Take(10);
                    index += 10;

                    if (page.Count() == 0)
                        break;

                    var resolved = await Task.WhenAll(page.Select(i => profileResolver.ResolvePersonFullProfileAsync(i)));
                    profiles.AddRange(resolved);
                }

                foreach (var item in personnelItems)
                {
                    if (item.AzureUniqueId.HasValue == false)
                        continue;

                    var profile = profiles.FirstOrDefault(p => p.AzureUniqueId == item.AzureUniqueId);
                    if (profile is null)
                        throw new InvalidOperationException($"Could locate profile for person with azure id {item.AzureUniqueId}. The profile should have been loaded...");


                    item.Positions = profile.Contracts.SelectMany(c => c.Positions.Select(p => new QueryOrgPositionInstance(c, p))).ToList();
                }

            }
        
            private async Task ExpandRequestsAsync(IEnumerable<QueryContractPersonnel> personnelItems)
            {
                var ids = personnelItems.Select(i => i.PersonnelId);

                var requests = await db.ContractorRequests
                    .Include(r => r.Person)
                    .Include(r => r.Contract)
                    .Include(r => r.Project)
                    .Where(r => ids.Contains(r.Person.Person.Id))
                    .ToListAsync();

                var basePositions = await Task.WhenAll(requests
                    .Select(q => q.Position.BasePositionId)
                    .Distinct()
                    .Select(bp => orgResolver.ResolveBasePositionAsync(bp))
                );

                var positions = requests.Select(p =>
                {
                    var position = new QueryPositionRequest(p.Position)
                        .WithResolvedBasePosition(basePositions.FirstOrDefault(bp => bp.Id == p.Position.BasePositionId));

                    return new QueryPersonnelRequestReference(p, position);
                }).ToList();


                foreach (var item in personnelItems)
                {
                    item.Requests = positions.Where(p => p.PersonnelId == item.PersonnelId).ToList();
                }
            }
        }
    }


}
