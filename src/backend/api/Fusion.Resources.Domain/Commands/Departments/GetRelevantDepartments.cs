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
            private readonly IMemoryCache memoryCache;
            private readonly HttpClient lineOrgClient;

            public Handler(ILogger<Handler> logger, IMediator mediator, IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
            {
                this.logger = logger;
                this.mediator = mediator;
                this.memoryCache = memoryCache;
                this.lineOrgClient = httpClientFactory.CreateClient("lineorg");
            }

            public async Task<QueryRelevantDepartments?> Handle(GetRelevantDepartments request, CancellationToken cancellationToken) 
                => await TryGetRelevantDepartmentsAsync(request.Department);


            private async Task<QueryRelevantDepartments?> TryGetRelevantDepartmentsAsync(string? fullDepartment)
            {
                if (fullDepartment is null)
                    return null;

                string cacheKey = $"relevant-departments-{fullDepartment}";

                if (memoryCache.TryGetValue(cacheKey, out QueryRelevantDepartments? relevantDepartments))
                    return relevantDepartments;

                try
                {
                    relevantDepartments = await ResolveRelevantDepartmentsAsync(fullDepartment);

                    memoryCache.Set(cacheKey, relevantDepartments, TimeSpan.FromHours(30));
                    return relevantDepartments;
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

                var currentDepartment = string.Join(" ", fullDepartmentPath.Split(" ").TakeLast(3));
                var parentDepartment = string.Join(" ", fullDepartmentPath.Split(" ").SkipLast(1).TakeLast(3));



                var respCurrentDepartment = lineOrgClient.GetAsync($"lineorg/departments/{currentDepartment}?$expand=children");
                var respParentDepartment = lineOrgClient.GetAsync($"lineorg/departments/{parentDepartment}?$expand=children");

                await Task.WhenAll(respCurrentDepartment, respParentDepartment);

                var respCurrent = await respCurrentDepartment;
                if (respCurrent.IsSuccessStatusCode)
                {
                    var content = await respCurrent.Content.ReadAsStringAsync();
                    var department = JsonConvert.DeserializeAnonymousType(content, new
                    {
                        children = new[] { new { name = string.Empty, fullName = string.Empty } }
                    });
                    var children = await mediator.Send(new GetDepartments().ByIds(department.children.Select(d => d.fullName)));
                    relevantDepartments.Children = children.ToList();
                }
                else if (respCurrent.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                var respParent = await respParentDepartment;
                if (respParent.IsSuccessStatusCode)
                {
                    var content = await respParent.Content.ReadAsStringAsync();
                    var department = JsonConvert.DeserializeAnonymousType(content, new
                    {
                        children = new[] { new { name = string.Empty, fullName = string.Empty } }
                    });
                    var siblings = await mediator.Send(new GetDepartments().ByIds(department.children.Select(d => d.fullName)));

                    relevantDepartments.Siblings = siblings.ToList();
                }

                return relevantDepartments;
            }
        }
    }
}
