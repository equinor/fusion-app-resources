using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetDepartmentUnassignedRequests : IRequest<QueryRangedList<QueryResourceAllocationRequest>>
    {
        public GetDepartmentUnassignedRequests(string departmentString)
        {
            DepartmentString = departmentString;
        }

        public string DepartmentString { get; }

        public class Handler : IRequestHandler<GetDepartmentUnassignedRequests, QueryRangedList<QueryResourceAllocationRequest>>
        {
            private readonly IMediator mediator;

            public Handler(IMediator mediator)
            {
                this.mediator = mediator;
            }

            public async Task<QueryRangedList<QueryResourceAllocationRequest>> Handle(GetDepartmentUnassignedRequests request, CancellationToken cancellationToken)
            {
                var unassignedRequests = await mediator.Send(new GetResourceAllocationRequests()
                    .ExpandPositions()
                    .ExpandPositionInstances()
                    .WithUnassignedFilter(true)
                    .ForResourceOwners()
                    .WithExcludeCompleted(true), cancellationToken) ;

                var sourceDepartmentLevel = request.DepartmentString.Split(" ").Length;

                // This should be some sort of configuration in the future
                bool departmentCheck(QueryResourceAllocationRequest r) =>
                    DepartmentUtils.IsRelevantBasePositionDepartment(sourceDepartmentLevel, request.DepartmentString, r.OrgPosition!.BasePosition.Department);

                var relevantRequests = unassignedRequests
                    .Where(r => r.OrgPosition != null && departmentCheck(r))
                    .ToList();

                return new QueryRangedList<QueryResourceAllocationRequest>(relevantRequests, relevantRequests.Count, 0);
            }
        }
    }
}
