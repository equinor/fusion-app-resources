using Fusion.Resources.Application.LineOrg;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetRelevantDepartments : IRequest<QueryRelevantDepartments?>
    {
        public GetRelevantDepartments(string department)
        {
            Department = department;
        }

        public string Department { get; }

        public class Handler : IRequestHandler<GetRelevantDepartments, QueryRelevantDepartments?>
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

            public async Task<QueryRelevantDepartments?> Handle(GetRelevantDepartments request, CancellationToken cancellationToken)
                => await TryGetRelevantDepartmentsAsync(request.Department);


            private async Task<QueryRelevantDepartments?> TryGetRelevantDepartmentsAsync(string? fullDepartment)
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

            private async Task<QueryRelevantDepartments?> ResolveRelevantDepartmentsAsync(string fullDepartmentPath)
            {
                var relevantDepartments = new QueryRelevantDepartments();

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
