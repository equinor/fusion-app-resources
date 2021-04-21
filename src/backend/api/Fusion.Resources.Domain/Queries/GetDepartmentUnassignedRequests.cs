using Fusion.Integration.Org;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class GetDepartmentUnassignedRequests : IRequest<IEnumerable<QueryResourceAllocationRequest>>
    {
        public GetDepartmentUnassignedRequests(string departmentString)
        {
            DepartmentString = departmentString;
        }

        public string DepartmentString { get; }

        public class Handler : IRequestHandler<GetDepartmentUnassignedRequests, IEnumerable<QueryResourceAllocationRequest>>
        {
            private readonly IMediator mediator;

            public Handler(IMediator mediator)
            {
                this.mediator = mediator;
            }

            public async Task<IEnumerable<QueryResourceAllocationRequest>> Handle(GetDepartmentUnassignedRequests request, CancellationToken cancellationToken)
            {
                var unassignedRequests = await mediator.Send(new GetResourceAllocationRequests()
                    .ExpandPositions()
                    .WithUnassignedFilter(true)
                    .WithExcludeCompleted(true), cancellationToken);

                var relevantRequests = unassignedRequests
                    .Where(r => r.OrgPosition != null && IsRelevantBasePosition(request.DepartmentString, r.OrgPosition.BasePosition.Department))
                    .ToList();

                return relevantRequests;
            }

            private static bool IsRelevantBasePosition(string sourceDepartment, string basePositionDepartment)
            {
                if (sourceDepartment is null || basePositionDepartment is null)
                    return false;

                if (sourceDepartment.StartsWith(basePositionDepartment, System.StringComparison.OrdinalIgnoreCase))
                    return true;

                if (basePositionDepartment.StartsWith(sourceDepartment, System.StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }
        }
    }
}
