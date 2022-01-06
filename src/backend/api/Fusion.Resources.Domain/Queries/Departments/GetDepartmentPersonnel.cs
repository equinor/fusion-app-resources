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
                var departmentRequests = await GetProposedRequestsAsync(request.Department);
                var requestsWithStateNullOrCreated = await GetRequestsWithStateNullOrCreatedAsync(request.Department);
                var departmentPersonnel = await GetDepartmentFromSearchIndexAsync(request.Department, request.includeSubdepartments, requestsWithStateNullOrCreated);
                var departmentAbsence = await GetPersonsAbsenceAsync(departmentPersonnel.Select(p => p.AzureUniqueId));
                
                departmentPersonnel.ForEach(p =>
                {
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

                return departmentPersonnel;
            }

            private async Task<Dictionary<Guid, List<QueryResourceAllocationRequest>>> GetProposedRequestsAsync(string department)
            {
                var command = new GetResourceAllocationRequests()
                    .WithAssignedDepartment(department)
                    .ExpandPositions()
                    .ExpandPositionInstances()
                    .ForAll()
                    .WithExcludeCompleted(true)
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
                if (department is null) return new List<QueryInternalPersonnelPerson>();

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                List<QueryInternalPersonnelPerson> personnel;

                if (includeSubDepartments || department.LineOrgResponsible?.AzureUniqueId is null)
                {
                    personnel = await PeopleSearchUtils.GetDepartmentFromSearchIndexAsync(peopleClient, requests,  fullDepartmentString);
                }
                else
                {
                    personnel = await PeopleSearchUtils.GetDirectReportsTo(peopleClient, department.LineOrgResponsible.AzureUniqueId.Value, requests);
                }

                return personnel;
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
