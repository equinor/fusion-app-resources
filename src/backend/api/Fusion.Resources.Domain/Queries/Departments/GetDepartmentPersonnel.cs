﻿using FluentValidation;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Http;
using Fusion.Resources.Database;
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

        private bool includeSubdepartments  = false;
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
                var departmentPersonnel = await GetDepartmentFromSearchIndexAsync(request.Department, request.includeSubdepartments);
                var departmentAbsence = await GetPersonsAbsenceAsync(departmentPersonnel.Select(p => p.AzureUniqueId));


                departmentPersonnel.ForEach(p =>
                {
                    p.Absence = departmentAbsence[p.AzureUniqueId];

                    if (request.ExpandTimeline)
                    {
                        // Timeline date input has been verified when shouldExpandTimline is true.
                        p.Timeline = TimelineUtils.GeneratePersonnelTimeline(p.PositionInstances, p.Absence, request.TimelineStart!.Value, request.TimelineEnd!.Value).OrderBy(p => p.AppliesFrom)
                            //.Where(t => (t.AppliesTo - t.AppliesFrom).Days > 2) // We do not want 1 day intervals that occur due to from/to do not overlap
                            .ToList();
                    }
                });

                return departmentPersonnel;
            }


            private async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(string fullDepartmentString, bool includeSubDepartments)
            {
                var department = await mediator.Send(new GetDepartment(fullDepartmentString));
                if (department is null) return new List<QueryInternalPersonnelPerson>();

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                List<QueryInternalPersonnelPerson> personnel;

                if (includeSubDepartments || department.LineOrgResponsible?.AzureUniqueId is null)
                {
                    personnel = await PeopleSearchUtils.GetDepartmentFromSearchIndexAsync(peopleClient, fullDepartmentString);
                }
                else
                {
                    personnel = await PeopleSearchUtils.GetDirectReportsTo(peopleClient, department.LineOrgResponsible.AzureUniqueId.Value);
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
