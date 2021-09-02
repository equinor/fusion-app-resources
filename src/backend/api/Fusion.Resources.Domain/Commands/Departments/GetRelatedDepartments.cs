using Fusion.Integration;
using Fusion.Integration.LineOrg;
using Fusion.Integration.Profile;
using Fusion.Resources.Application;
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
            private readonly IFusionProfileResolver profileService;

            public Handler(ILogger<Handler> logger, IMediator mediator, ILineOrgResolver lineOrgResolver, IFusionProfileResolver profileService)
            {
                this.logger = logger;
                this.mediator = mediator;
                this.lineOrgResolver = lineOrgResolver;
                this.profileService = profileService;
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
                var department = await lineOrgResolver.ResolveDepartmentAsync(fullDepartmentPath);
                if (department is null) return null;

                var children = await lineOrgResolver.ResolveDepartmentChildrenAsync(department);
                if (children is not null)
                {
                    relevantDepartments.Children = await children.ToQueryDepartment(profileService);
                }

                var siblings = await lineOrgResolver.ResolveDepartmentChildrenAsync(string.Join(" ", fullDepartmentPath.Split(" ").SkipLast(1)));
                if(siblings is not null)
                {
                    relevantDepartments.Siblings = await siblings.ToQueryDepartment(profileService);
                }

                return relevantDepartments;
            }

           
        }
    }
}
