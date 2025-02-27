using FluentValidation;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Http;
using Fusion.Resources.Database;
using Fusion.Resources.Domain.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{

    public class GetDepartmentPersonnel : IRequest<IEnumerable<QueryInternalPersonnelPerson>>
    {

        public GetDepartmentPersonnel(string department, ODataQueryParams? queryParams = null)
        {
            Department = department;
            QueryParams = queryParams;
        }

        private bool includeSubdepartments;
        private bool includeCurrentAllocations;
        public bool ExpandTimeline { get; set; }
        public string Department { get; set; }
        public ODataQueryParams? QueryParams { get; }

        public DateTime? TimelineStart { get; set; }
        public DateTime? TimelineEnd { get; set; }

        public int? Version { get; set; }


        public GetDepartmentPersonnel WithTimeline(bool shouldExpandTimeline, DateTime? start, DateTime? end)
        {
            ExpandTimeline = shouldExpandTimeline;
            TimelineStart = start;
            TimelineEnd = end;

            return this;
        }

        public GetDepartmentPersonnel IncludeSubdepartments(bool includeSubdepartments)
        {
            this.includeSubdepartments = includeSubdepartments;
            return this;
        }

        public GetDepartmentPersonnel IncludeCurrentAllocations(bool includeCurrentAllocations)
        {
            this.includeCurrentAllocations = includeCurrentAllocations;
            return this;
        }

        public GetDepartmentPersonnel WithVersion(int? version)
        {
            Version = version;
            return this;
        }

        public class Validator : AbstractValidator<GetDepartmentPersonnel>
        {
            public Validator()
            {
                RuleFor(x => x.TimelineStart).NotNull().When(x => x.ExpandTimeline);
                RuleFor(x => x.TimelineEnd).NotNull().When(x => x.ExpandTimeline);
                RuleFor(x => x.TimelineEnd).GreaterThan(x => x.TimelineStart).When(x => x.ExpandTimeline);
                RuleFor(x => x.Department).NotEmpty().WithMessage("Full department string must be provided");
            }
        }

        public class Handler : IRequestHandler<GetDepartmentPersonnel, IEnumerable<QueryInternalPersonnelPerson>>
        {
            private readonly ResourcesDbContext db;
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IMediator mediator;

            public Handler(ResourcesDbContext db, IHttpClientFactory httpClientFactory, IMediator mediator)
            {
                this.db = db;
                this.httpClientFactory = httpClientFactory;
                this.mediator = mediator;
            }

            public async Task<IEnumerable<QueryInternalPersonnelPerson>> Handle(GetDepartmentPersonnel request, CancellationToken cancellationToken)
            {
                var departmentRequests = await GetPendingRequests(request.Department);
                var requestsWithStateNullOrCreated = await GetRequestsWithStateNullOrCreatedAsync(request.Department);
                var departmentPersonnel = await GetDepartmentFromSearchIndexAsyncV2(request.Department, requestsWithStateNullOrCreated);


                var departmentAbsence = await GetPersonsAbsenceAsync(departmentPersonnel.Select(p => p.AzureUniqueId));

                departmentPersonnel.ForEach(p =>
                {
                    // Filter out all positions of type products
                    p.PositionInstances = p.PositionInstances.Where(pis => pis.BasePosition != null && pis.BasePosition.ProjectType != null && !pis.BasePosition.ProjectType.Equals("Product")).ToList();

                    p.Absence = departmentAbsence[p.AzureUniqueId];
                    if (departmentRequests.ContainsKey(p.AzureUniqueId))
                        p.PendingRequests = departmentRequests[p.AzureUniqueId];

                    if (request.ExpandTimeline)
                    {
                        p.PositionInstances = p.PositionInstances
                            .Where(instance => instance.AppliesTo >= request.TimelineStart && instance.AppliesFrom <= request.TimelineEnd)
                            .ToList();

                        p.Absence = p.Absence
                                     .Where(instance => (instance.AppliesTo == null || instance.AppliesTo >= request.TimelineStart) && instance.AppliesFrom <= request.TimelineEnd)
                                     .ToList();

                        if (p.PendingRequests != null)
                            p.PendingRequests = p.PendingRequests.Where(instance => instance.OrgPositionInstance?.AppliesTo >= request.TimelineStart && instance.OrgPositionInstance?.AppliesFrom <= request.TimelineEnd).ToList();

                        // Timeline date input has been verified when shouldExpandTimline is true.
                        p.Timeline = new PersonnelTimelineBuilder(request.TimelineStart!.Value, request.TimelineEnd!.Value)
                                     .WithPositions(p.PositionInstances)
                                     .WithAbsences(p.Absence)
                                     .WithPendingRequests(p.PendingRequests)
                                     .Build();

                    }

                    if (request.includeCurrentAllocations)
                    {
                        p.PositionInstances = p.PositionInstances
                                               .Where(instance => instance.AppliesTo >= DateTime.Now && instance.AppliesFrom <= DateTime.Now)
                                               .ToList();

                        p.Absence = p.Absence
                                     .Where(instance => (instance.AppliesTo == null || instance.AppliesTo >= DateTime.Now) && instance.AppliesFrom <= DateTime.Now)
                                     .ToList();

                        if (p.PendingRequests != null)
                        {
                            p.PendingRequests = p.PendingRequests.Where(instance => instance.OrgPositionInstance?.AppliesTo >= DateTime.Now && instance.OrgPositionInstance?.AppliesFrom <= DateTime.Now).ToList();
                        }
                    }
                });

                await PopulateProjectStateForPositionInstancesAsync(departmentPersonnel, cancellationToken);


                return departmentPersonnel;
            }

            private async Task<Dictionary<Guid, List<QueryResourceAllocationRequest>>> GetPendingRequests(string department)
            {
                var command = new GetResourceAllocationRequests()
                    .WithAssignedDepartment(department)
                    .ExpandPositions()
                    .ExpandPositionInstances()
                    .ForAll()
                    .WithExcludeCompleted()
                    .WithExcludeWithoutProposedPerson();
                var pendingRequests = await mediator.Send(command);
                return pendingRequests
                    .ToLookup(x => x.ProposedPerson!.AzureUniqueId)
                    .ToDictionary(x => x.Key, x => x.ToList());
            }

            private async Task<List<QueryResourceAllocationRequest>> GetRequestsWithStateNullOrCreatedAsync(string department)
            {
                var command = new GetResourceAllocationRequests()
                              .ForResourceOwners()
                              .WithAssignedDepartment(department)
                              .ExpandPositions()
                              .ExpandPositionInstances();
                var requests = await mediator.Send(command);

                return requests.Where(r => string.IsNullOrWhiteSpace(r.State) || r.State == "created").ToList();
            }

            private async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(string fullDepartmentString, bool includeSubDepartments, List<QueryResourceAllocationRequest> requests)
            {
                var department = await mediator.Send(new GetDepartment(fullDepartmentString));
                if (department is null)
                    return new List<QueryInternalPersonnelPerson>();

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                List<QueryInternalPersonnelPerson> personnel;

                if (includeSubDepartments || department.LineOrgResponsible?.AzureUniqueId is null)
                {
                    personnel = await PeopleSearchUtils.GetDepartmentFromSearchIndexAsync(peopleClient, requests, fullDepartmentString);
                }
                else
                {
                    personnel = await PeopleSearchUtils.GetDirectReportsTo(peopleClient, department.LineOrgResponsible.AzureUniqueId.Value, requests);
                }

                return personnel;
            }

            private async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsyncV2(string fullDepartmentString, List<QueryResourceAllocationRequest> requests)
            {
                // Not sure what includeSubDepartments does tbh.. By looking at existing code, it doesn't do much other than what we want to do.. 
                // I guess the different might be that it lists persons cross departments, when the manger is acting for different org units.

                var orgUnit = await mediator.Send(new ResolveLineOrgUnit(fullDepartmentString));


                // Copied from line org for now. Might want to decorate the profiles in the search index with the department property so the query is simple
                // Will be too many round-trips to ask line org for 

                //var managers = orgUnit?.Management?.Persons?
                //    .Where(m => string.Equals(m.FullDepartment, orgUnit.FullDepartment, StringComparison.OrdinalIgnoreCase))
                //    .Select(m => m.AzureUniqueId)
                //    .ToList() ?? new List<Guid>();

                //var removeManagerQuery = string.Join(" and ", managers.Select(m => $"azureUniqueId ne '{m}'"));
                //var queryString = (managers.Any() ? removeManagerQuery + " and " : "") + $"fullDepartment eq '{fullDepartmentString}' and isExpired eq false";


                //if (managers.Any())
                //    queryString += " or " + string.Join(" or ", managers.Select(m => $"managerAzureId eq '{m}' and isResourceOwner eq true"));

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                var personnel = await PeopleSearchUtils.GetFromSearchIndexAsync(peopleClient, $"orgUnitId eq '{orgUnit?.SapId}' and isExpired eq false", requests: requests);


                return personnel;
            }

            private async Task PopulateProjectStateForPositionInstancesAsync(
                List<QueryInternalPersonnelPerson> departmentPersonnel, CancellationToken cancellationToken = default)
            {
                var orgProjectIds = departmentPersonnel
                    .SelectMany(d => d.PositionInstances.Select(p => p.Project.OrgProjectId))
                    .Distinct();


                var orgProjectIdsDictionary = await db.Projects
                    .Select(p => new { p.OrgProjectId, p.State })
                    .Where(p => orgProjectIds.Contains(p.OrgProjectId))
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.OrgProjectId, p => p.State, cancellationToken: cancellationToken);


                departmentPersonnel.ForEach(p =>
                {
                    p.PositionInstances.ForEach(pi =>
                    {
                        pi.Project.State = orgProjectIdsDictionary.GetValueOrDefault(pi.Project.OrgProjectId);
                    });
                });
            }

            private async Task<Dictionary<Guid, List<QueryPersonAbsenceBasic>>> GetPersonsAbsenceAsync(IEnumerable<Guid> azureIds)
            {
                var ids = azureIds.ToArray();

                var items = await db.PersonAbsences.Where(a => ids.Contains(a.Person.AzureUniqueId))
                    .Include(a => a.TaskDetails)
                    .Select(a => new { absence = a, azureId = a.Person.AzureUniqueId })
                    .ToListAsync();

                var personsAbsence = ids.Select(pId => new { id = pId, items = items.Where(i => i.azureId == pId).Select(i => i.absence).ToList() })
                    .ToDictionary(i => i.id, i => i.items.Select(a => new QueryPersonAbsenceBasic(a)).ToList());

                return personsAbsence;
            }

        }
    }
}
