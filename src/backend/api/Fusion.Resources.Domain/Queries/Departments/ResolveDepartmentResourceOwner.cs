using Fusion.Services.LineOrg.ApiModels;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    /// <summary>
    /// Returns null if org unit is not found. If there are no managers the MainManager will be null.
    /// </summary>
    public class ResolveDepartmentResourceOwner : IRequest<QueryOrgUnitResourceOwner?>
    {
        public string DepartmentId { get; }

        /// <summary>
        /// Resolve org unit from line org from either full department string or sap id.
        /// </summary>
        public ResolveDepartmentResourceOwner(string departmentId)
        {
            DepartmentId = departmentId;
        }

        public class Handler : IRequestHandler<ResolveDepartmentResourceOwner, QueryOrgUnitResourceOwner?>
        {
            private readonly IMediator mediator;

            public Handler(IMediator mediator)
            {
                this.mediator = mediator;
            }

            public async Task<QueryOrgUnitResourceOwner?> Handle(ResolveDepartmentResourceOwner request, CancellationToken cancellationToken)
            {
                var orgUnit = await mediator.Send(new ResolveLineOrgUnit(request.DepartmentId));

                if (orgUnit is null)
                {
                    return null;
                }

                // Find resource owner - For now just use the first in the management attribute. 

                var manager = orgUnit?.Management?.Persons.FirstOrDefault();

                return new QueryOrgUnitResourceOwner(orgUnit!.SapId, orgUnit.FullDepartment)
                {
                    MainManager = manager,
                    AllManagers = orgUnit.Management?.Persons.ToList() ?? new List<ApiPerson>(),
                };
            }
        }
    }

}