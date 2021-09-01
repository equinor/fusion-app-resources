using Fusion.Resources.Application.LineOrg;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetRelatedDepartments : IRequest<QueryRelatedDepartments?>
    {
        public GetRelatedDepartments(string department)
        {
            Department = department;
        }

        public string Department { get; }

        public class Handler : IRequestHandler<GetRelatedDepartments, QueryRelatedDepartments?>
        {
            private readonly ILogger<Handler> logger;
            private readonly IMediator mediator;
            private readonly ILineOrgResolver lineOrgResolver;

            public Handler(ILogger<Handler> logger, IMediator mediator, ILineOrgResolver lineOrgResolver)
            {
                this.logger = logger;
                this.mediator = mediator;
                this.lineOrgResolver = lineOrgResolver;
            }

            public async Task<QueryRelatedDepartments?> Handle(GetRelatedDepartments request, CancellationToken cancellationToken)
                => await TryGetRelevantDepartmentsAsync(request.Department);


            private async Task<QueryRelatedDepartments?> TryGetRelevantDepartmentsAsync(string? fullDepartment)
            {
                if (fullDepartment is null)
                    return null;

                try
                {
                    return await ResolveRelevantDepartmentsAsync(fullDepartment);

                }
                catch (Exception ex)
                {
                    // Log
                    logger.LogCritical(ex, "Could not resolve relevant departments for {Department}", fullDepartment);
                    return null;
                }
            }

            private async Task<QueryRelatedDepartments?> ResolveRelevantDepartmentsAsync(string fullDepartmentPath)
            {
                var relevantDepartments = new QueryRelatedDepartments();

                var children = await lineOrgResolver.GetChildren(fullDepartmentPath);
                if (children is null) return null;

                var siblings = await lineOrgResolver.GetChildren(string.Join(" ", fullDepartmentPath.Split(" ").SkipLast(1)));
                relevantDepartments.Children = children.Select(x => new QueryDepartment(x)).ToList();
                relevantDepartments.Siblings = siblings?.Select(x => new QueryDepartment(x))?.ToList() ?? new List<QueryDepartment>();

                return relevantDepartments;
            }
        }
    }
}
