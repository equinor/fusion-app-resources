﻿using FluentValidation;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Http;
using Fusion.Resources.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetSectorPersonnel : IRequest<IEnumerable<QueryInternalPersonnelPerson>>
    {

        public GetSectorPersonnel(string department, ODataQueryParams? queryParams = null)
        {
            Department = department;
            QueryParams = queryParams;
        }


        public bool ExpandTimeline { get; set; }
        public string Department { get; set; }
        public ODataQueryParams? QueryParams { get; }

        public DateTime? TimelineStart { get; set; }
        public DateTime? TimelineEnd { get; set; }


        public GetSectorPersonnel WithTimeline(bool shouldExpandTimeline, DateTime? start, DateTime? end)
        {
            ExpandTimeline = shouldExpandTimeline;
            TimelineStart = start;
            TimelineEnd = end;

            return this;
        }


        public class Validator : AbstractValidator<GetSectorPersonnel>
        {
            public Validator()
            {
                RuleFor(x => x.TimelineStart).NotNull().When(x => x.ExpandTimeline);
                RuleFor(x => x.TimelineEnd).NotNull().When(x => x.ExpandTimeline);
                RuleFor(x => x.TimelineEnd).GreaterThan(x => x.TimelineStart).When(x => x.ExpandTimeline);
                RuleFor(x => x.Department).NotEmpty().WithMessage("Full department string must be provided");
            }
        }

        public class Handler : IRequestHandler<GetSectorPersonnel, IEnumerable<QueryInternalPersonnelPerson>>
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

            public async Task<IEnumerable<QueryInternalPersonnelPerson>> Handle(GetSectorPersonnel request, CancellationToken cancellationToken)
            {
                var departmentPersonnel = await GetDepartmentFromSearchIndexAsync(request.Department);
                var departmentAbsence = await GetPersonsAbsenceAsync(departmentPersonnel.Select(p => p.AzureUniqueId));

                departmentPersonnel.ForEach(p =>
                {
                    p.Absence = departmentAbsence[p.AzureUniqueId];

                    if (request.ExpandTimeline)
                    {
                        // Timeline date input has been verified when shouldExpandTimline is true.
                        p.Timeline = new PersonnelTimelineBuilder(request.TimelineStart!.Value, request.TimelineEnd!.Value)
                            .WithPositions(p.PositionInstances)
                            .WithAbsences(p.Absence)
                            .Build();
                    }
                });

                return departmentPersonnel;
            }


            private async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(string fullDepartmentString)
            {
                var departments = await mediator.Send(new GetDepartments().InSector(fullDepartmentString));
                var managerIds = new HashSet<Guid>(
                    departments
                        .Where(x => x.LineOrgResponsible?.AzureUniqueId != null)
                        .Select(x => x.LineOrgResponsible!.AzureUniqueId!.Value)
                );

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);
                var sectorPersonnel = await PeopleSearchUtils.GetDepartmentFromSearchIndexAsync(peopleClient, departments.Select(x => x.Identifier).Where(i => !string.IsNullOrEmpty(i))!);

                return sectorPersonnel
                    .Where(x => x.ManagerAzureId != null && managerIds.Contains(x.ManagerAzureId.Value))
                    .ToList();
            }

            private async Task<Dictionary<Guid, List<QueryPersonAbsenceBasic>>> GetPersonsAbsenceAsync(IEnumerable<Guid> azureIds)
            {
                var ids = azureIds.ToArray();

                var items = await db.PersonAbsences.Where(a => ids.Contains(a.Person.AzureUniqueId))
                    .Select(a => new { absence = a, azureId = a.Person.AzureUniqueId })
                    .ToListAsync();

                var personsAbsence = ids.Select(pId => new { id = pId, items = items.Where(i => i.azureId == pId).Select(i => i.absence).ToList() })
                    .ToDictionary(i => i.id, i => i.items.Select(a => new QueryPersonAbsenceBasic(a)).ToList());

                return personsAbsence;
            }
        }
    }
}
