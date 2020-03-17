using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Domain
{
    public class GetContractPersonnelItem : IRequest<QueryContractPersonnel>
    {

        public GetContractPersonnelItem(Guid orgContractId, PersonnelId personnelId)
        {
            OrgContractId = orgContractId;
            PersonnelId = personnelId;
        }

        public Guid OrgContractId { get; set; }
        public PersonnelId PersonnelId { get; }


        public class Handler : IRequestHandler<GetContractPersonnelItem, QueryContractPersonnel>
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

            public async Task<QueryContractPersonnel> Handle(GetContractPersonnelItem request, CancellationToken cancellationToken)
            {
                var item = await db.ContractPersonnel
                    .GetById(request.OrgContractId, request.PersonnelId)
                    .Include(i => i.Contract)
                    .Include(i => i.Project)
                    .Include(i => i.UpdatedBy)
                    .Include(i => i.CreatedBy)
                    .Include(i => i.Person).ThenInclude(p => p.Disciplines)
                    .FirstOrDefaultAsync();

                if (item == null)
                    return null;

                var returnItem = new QueryContractPersonnel(item);

                await ExpandRequestsAsync(returnItem);
                await ExpandPositionsAsync(returnItem);

                return returnItem;
            }

            private async Task ExpandPositionsAsync(QueryContractPersonnel personnelItem)
            {
                if (personnelItem.AzureUniqueId == null)
                    return;

                var profile = await profileResolver.ResolvePersonFullProfileAsync(personnelItem.AzureUniqueId.Value);
                personnelItem.Positions = profile.Contracts.SelectMany(c => c.Positions.Select(p => new QueryOrgPositionInstance(c, p))).ToList();
            }

            private async Task ExpandRequestsAsync(QueryContractPersonnel personnelItem)
            {

                var requests = await db.ContractorRequests
                    .Include(r => r.Person)
                    .Include(r => r.Contract)
                    .Include(r => r.Project)
                    .Where(r => r.Person.Person.Id == personnelItem.PersonnelId)
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


                personnelItem.Requests = positions;
            }
        }
    }


}
