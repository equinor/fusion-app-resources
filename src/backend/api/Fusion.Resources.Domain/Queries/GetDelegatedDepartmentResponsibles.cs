using System;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Integration;
using Fusion.Resources.Domain.Models;
using Itenso.TimePeriod;

namespace Fusion.Resources.Domain
{
    public class GetDelegatedDepartmentResponsibles : IRequest<IEnumerable<QueryDepartmentResponsible>>
    {
        public GetDelegatedDepartmentResponsibles(string departmentId)
        {
            DepartmentId = departmentId;
        }

        private string DepartmentId { get; set; }


        public class Handler : IRequestHandler<GetDelegatedDepartmentResponsibles, IEnumerable<QueryDepartmentResponsible>>
        {
            private readonly ResourcesDbContext db;
            private readonly IFusionProfileResolver profileResolver;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IFusionProfileResolver profileResolver, IMediator mediator)
            {
                this.db = db;
                this.profileResolver = profileResolver;
                this.mediator = mediator;
            }

            public async Task<IEnumerable<QueryDepartmentResponsible>> Handle(GetDelegatedDepartmentResponsibles request, CancellationToken cancellationToken)
            {
                var returnModel = new List<QueryDepartmentResponsible>();

                var department = await mediator.Send(new GetDepartment(request.DepartmentId));
                var  today = DateTime.Today.ToUniversalTime();
                if (department is null)
                    return returnModel;

                var delegatedResourceOwners = await db.DelegatedDepartmentResponsibles
                    .Where(r => r.DepartmentId == request.DepartmentId &&
                    r.DateFrom.Date <= DateTime.Today && r.DateTo.Date >= DateTime.Today)
          
                    .ToListAsync(cancellationToken);

                foreach (var m in delegatedResourceOwners)
                {
                    var personDelegated = await profileResolver.ResolvePersonBasicProfileAsync(m.ResponsibleAzureObjectId);
                    var item = new QueryDepartmentResponsible(m) { DelegatedResponsible = personDelegated };

                    if (m.UpdatedBy.HasValue)
                    {
                        var personAssignedBy = await profileResolver.ResolvePersonBasicProfileAsync(m.UpdatedBy);
                        item.CreatedBy = personAssignedBy;
                    }

                    returnModel.Add(item);
                }

                return returnModel;
            }
        }
    }
}