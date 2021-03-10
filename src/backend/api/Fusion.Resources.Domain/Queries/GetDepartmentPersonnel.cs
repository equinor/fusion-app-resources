using FluentValidation;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Http;
using Fusion.Resources.Database;
using Itenso.TimePeriod;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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

            public Handler(ResourcesDbContext db, IHttpClientFactory httpClientFactory)
            {
                this.db = db;
                this.httpClientFactory = httpClientFactory;
            }

            public async Task<IEnumerable<QueryInternalPersonnelPerson>> Handle(GetDepartmentPersonnel request, CancellationToken cancellationToken)
            {
                var departmentPersonnel = await GetDepartmentFromSearchIndexAsync(request.Department);
                var departmentAbsence = await GetPersonsAbsenceAsync(departmentPersonnel.Select(p => p.AzureUniqueId));


                departmentPersonnel.ForEach(p =>
                {
                    p.Absence = departmentAbsence[p.AzureUniqueId];

                    if (request.ExpandTimeline)
                    {
                        // Timeline date input has been verified when shouldExpandTimline is true.
                        p.Timeline = TimelineUtils.GeneratePersonnelTimeline(p.PositionInstances, p.Absence, request.TimelineStart!.Value, request.TimelineEnd!.Value).OrderBy(p => p.AppliesFrom)
                            .Where(t => (t.AppliesTo - t.AppliesFrom).Days > 2) // We do not want 1 day intervals that occur due to from/to do not overlap
                            .ToList();
                    }
                });

                return departmentPersonnel;
            }


            private async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(string fullDepartmentString)
            {
                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                var departmentPersonnel = await PeopleSearchUtils.GetDepartmentFromSearchIndexAsync(peopleClient, fullDepartmentString);
                return departmentPersonnel;
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
