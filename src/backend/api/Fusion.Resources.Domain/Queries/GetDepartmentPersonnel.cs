using FluentValidation;
using Fusion.AspNetCore.OData;
using Fusion.Integration.Http;
using Fusion.Resources.Database;
using Itenso.TimePeriod;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetDepartmentPersonnel : IRequest<IEnumerable<QueryDepartmentPersonnelPerson>>
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

        public class Handler : IRequestHandler<GetDepartmentPersonnel, IEnumerable<QueryDepartmentPersonnelPerson>>
        {
            private readonly ResourcesDbContext db;
            private readonly IHttpClientFactory httpClientFactory;

            public Handler(ResourcesDbContext db, IHttpClientFactory httpClientFactory)
            {
                this.db = db;
                this.httpClientFactory = httpClientFactory;
            }

            public async Task<IEnumerable<QueryDepartmentPersonnelPerson>> Handle(GetDepartmentPersonnel request, CancellationToken cancellationToken)
            {
                var departmentPersonnel = await GetDepartmentFromSearchIndexAsync(request.Department);
                var departmentAbsence = await GetPersonsAbsenceAsync(departmentPersonnel.Select(p => p.AzureUniqueId));


                departmentPersonnel.ForEach(p =>
                {
                    p.Absence = departmentAbsence[p.AzureUniqueId];

                    if (request.ExpandTimeline)
                    {
                        // Timeline date input has been verified when shouldExpandTimline is true.
                        p.Timeline = GenerateTimeline(p.PositionInstances, p.Absence, request.TimelineStart!.Value, request.TimelineEnd!.Value).OrderBy(p => p.AppliesFrom)
                            .Where(t => (t.AppliesTo - t.AppliesFrom).Days > 2) // We do not want 1 day intervals that occur due to from/to do not overlap
                            .ToList();

                        // Tweek ranges where end date == next start date
                        var indexToMoveBack = new List<int>();
                        for (int i = 0; i < p.Timeline.Count; i++)
                        {
                            var now = p.Timeline.ElementAt(i);
                            var next = p.Timeline.ElementAtOrDefault(i + 1);

                            if (next != null && now.AppliesTo == next.AppliesFrom)
                                now.AppliesTo = now.AppliesTo.Subtract(TimeSpan.FromDays(1));
                        }
                    }
                });

                return departmentPersonnel;
            }


            private async Task<List<QueryDepartmentPersonnelPerson>> GetDepartmentFromSearchIndexAsync(string fullDepartmentString)
            {
                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                var response = await peopleClient.PostAsJsonAsync("/search/persons/query", new
                {
                    filter = $"fullDepartment eq '{fullDepartmentString}'"
                });

                var data = await response.Content.ReadAsStringAsync();

                var items = JsonConvert.DeserializeAnonymousType(data, new
                {
                    results = new[]
                    {
                    new {
                        document = new
                        {
                            azureUniqueId = Guid.Empty,
                            mail = string.Empty,
                            name = string.Empty,
                            jobTitle = string.Empty,
                            department = string.Empty,
                            fullDepartment = string.Empty,
                            mobilePhone = string.Empty,
                            officeLocation = string.Empty,
                            upn = string.Empty,
                            accountType = string.Empty,
                            isExpired = false,

                            positions = new [] {
                                new {
                                    id = Guid.Empty,
                                    instanceId = Guid.Empty,
                                    name = string.Empty,
                                    appliesFrom = (DateTime?) null,
                                    appliesTo = (DateTime?) null,
                                    isActive = false,
                                    obs = string.Empty,
                                    locationName = string.Empty,
                                    workload = 0.0,
                                    project = new
                                    {
                                        name = string.Empty,
                                        id = Guid.Empty,
                                        domainId = string.Empty
                                    },

                                    basePosition = new
                                    {
                                        id = Guid.Empty,
                                        name = string.Empty,
                                        discipline = string.Empty,
                                        projectType = string.Empty
                                    }
                                }

                            }
                        }
                    }
                }
                });

                var departmentPersonnel = items.results.Select(i => new QueryDepartmentPersonnelPerson(i.document.azureUniqueId, i.document.mail, i.document.name, i.document.accountType)
                {
                    PhoneNumber = i.document.mobilePhone,
                    JobTitle = i.document.jobTitle,
                    OfficeLocation = i.document.officeLocation,
                    PositionInstances = i.document.positions.Select(p => new QueryDepartmentPersonnelPerson.PersonnelPosition
                    {
                        PositionId = p.id,
                        InstanceId = p.instanceId,
                        AppliesFrom = p.appliesFrom!.Value,
                        AppliesTo = p.appliesTo!.Value,
                        Name = p.name,
                        Location = p.locationName,
                        BasePosition = new QueryBasePosition(p.basePosition.id, p.basePosition.name, p.basePosition.discipline, p.basePosition.projectType),
                        Project = new QueryProjectRef(p.project.id, p.project.name, p.project.domainId),
                        Workload = p.workload
                    }).OrderBy(p => p.AppliesFrom).ToList()
                }).ToList();

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

            private IEnumerable<QueryTimelineRange<QueryDepartmentPersonnelPerson.PersonnelTimelineItem>> GenerateTimeline(
                List<QueryDepartmentPersonnelPerson.PersonnelPosition> position, 
                List<QueryPersonAbsenceBasic> absences, 
                DateTime filterStart, 
                DateTime filterEnd)
            {
                // Ensure utc dates
                if (filterStart.Kind != DateTimeKind.Utc)
                    filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

                if (filterEnd.Kind != DateTimeKind.Utc)
                    filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);


                // Gather all dates 
                var dates = position.SelectMany(p => new[] { (DateTime?)p.AppliesFrom.Date, (DateTime?)p.AppliesTo.Date })
                    .Union(absences.SelectMany(a => new[] { a.AppliesFrom.Date, a.AppliesTo?.Date}))
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                if (!dates.Any())
                    yield break;

                var validDates = dates.Where(d => d > filterStart && d < filterEnd).ToList();

                validDates.Insert(0, filterStart);
                validDates.Add(filterEnd);

                var current = validDates.First();
                foreach (var date in validDates.Skip(1))
                {
                    var timelineRange = new TimeRange(current, date);

                    //bool overlap = a.start < b.end && b.start < a.end;

                    var affectedItems = position.Where(p =>
                    {
                        var posTimeRange = new TimeRange(p.AppliesFrom.Date, p.AppliesTo.Date);
                        return posTimeRange.OverlapsWith(timelineRange);
                    });
                    var relevantAbsence = absences.Where(p => p.AppliesTo.HasValue && timelineRange.OverlapsWith(new TimeRange(p.AppliesFrom.Date, p.AppliesTo!.Value.Date)));

                    yield return new QueryTimelineRange<QueryDepartmentPersonnelPerson.PersonnelTimelineItem>(current, date)
                    {
                        Items = affectedItems.Select(p => new QueryDepartmentPersonnelPerson.PersonnelTimelineItem()
                        {
                            Type = "PositionInstance",
                            Workload = p.Workload,
                            Id = p.PositionId,
                            Description = $"{p.Name}",
                            BasePosition = p.BasePosition,
                            Project = p.Project
                        }).Union(relevantAbsence.Select(a => new QueryDepartmentPersonnelPerson.PersonnelTimelineItem()
                        {
                            Id = a.Id,
                            Type = "Absence",
                            Workload = a.AbsencePercentage,
                            Description = $"{a.Type}"
                        }))
                        .ToList(),
                        Workload = affectedItems.Sum(p => p.Workload) + relevantAbsence.Where(a => a.AbsencePercentage.HasValue).Sum(a => a.AbsencePercentage!.Value)
                    };

                    current = date;
                }
            }
        }
    }
    public class GetSectorPersonnel : IRequest<IEnumerable<QueryDepartmentPersonnelPerson>>
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

        public class Handler : IRequestHandler<GetSectorPersonnel, IEnumerable<QueryDepartmentPersonnelPerson>>
        {
            private readonly ResourcesDbContext db;
            private readonly IHttpClientFactory httpClientFactory;

            public Handler(ResourcesDbContext db, IHttpClientFactory httpClientFactory)
            {
                this.db = db;
                this.httpClientFactory = httpClientFactory;
            }

            public async Task<IEnumerable<QueryDepartmentPersonnelPerson>> Handle(GetSectorPersonnel request, CancellationToken cancellationToken)
            {
                var departmentPersonnel = await GetDepartmentFromSearchIndexAsync(request.Department);
                var departmentAbsence = await GetPersonsAbsenceAsync(departmentPersonnel.Select(p => p.AzureUniqueId));


                departmentPersonnel.ForEach(p =>
                {
                    p.Absence = departmentAbsence[p.AzureUniqueId];

                    if (request.ExpandTimeline)
                    {
                        // Timeline date input has been verified when shouldExpandTimline is true.
                        p.Timeline = GenerateTimeline(p.PositionInstances, p.Absence, request.TimelineStart!.Value, request.TimelineEnd!.Value).OrderBy(p => p.AppliesFrom)
                            .Where(t => (t.AppliesTo - t.AppliesFrom).Days > 2) // We do not want 1 day intervals that occur due to from/to do not overlap
                            .ToList();

                        // Tweek ranges where end date == next start date
                        var indexToMoveBack = new List<int>();
                        for (int i = 0; i < p.Timeline.Count; i++)
                        {
                            var now = p.Timeline.ElementAt(i);
                            var next = p.Timeline.ElementAtOrDefault(i + 1);

                            if (next != null && now.AppliesTo == next.AppliesFrom)
                                now.AppliesTo = now.AppliesTo.Subtract(TimeSpan.FromDays(1));
                        }
                    }
                });

                return departmentPersonnel;
            }


            private async Task<List<QueryDepartmentPersonnelPerson>> GetDepartmentFromSearchIndexAsync(string fullDepartmentString)
            {
                var sectorInfo = LoadSectorInfo();
                if (!sectorInfo.TryGetValue(fullDepartmentString.ToUpper(), out string? sector))
                    throw new InvalidOperationException($"Could not locate any sector for the department '{fullDepartmentString}'");

                var departments = sectorInfo.Where(kv => kv.Value == sector && kv.Key != sector).Select(kv => kv.Key).ToList();


                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

                var response = await peopleClient.PostAsJsonAsync("/search/persons/query", new
                {
                    filter = string.Join(" or ", departments.Select(dep => $"fullDepartment eq '{dep}'"))
                });

                var data = await response.Content.ReadAsStringAsync();

                var items = JsonConvert.DeserializeAnonymousType(data, new
                {
                    results = new[]
                    {
                    new {
                        document = new
                        {
                            azureUniqueId = Guid.Empty,
                            mail = string.Empty,
                            name = string.Empty,
                            jobTitle = string.Empty,
                            department = string.Empty,
                            fullDepartment = string.Empty,
                            mobilePhone = string.Empty,
                            officeLocation = string.Empty,
                            upn = string.Empty,
                            accountType = string.Empty,
                            isExpired = false,

                            positions = new [] {
                                new {
                                    id = Guid.Empty,
                                    instanceId = Guid.Empty,
                                    name = string.Empty,
                                    appliesFrom = (DateTime?) null,
                                    appliesTo = (DateTime?) null,
                                    isActive = false,
                                    obs = string.Empty,
                                    locationName = string.Empty,
                                    workload = 0.0,
                                    project = new
                                    {
                                        name = string.Empty,
                                        id = Guid.Empty,
                                        domainId = string.Empty
                                    },

                                    basePosition = new
                                    {
                                        id = Guid.Empty,
                                        name = string.Empty,
                                        discipline = string.Empty,
                                        projectType = string.Empty
                                    }
                                }

                            }
                        }
                    }
                }
                });

                var departmentPersonnel = items.results.Select(i => new QueryDepartmentPersonnelPerson(i.document.azureUniqueId, i.document.mail, i.document.name, i.document.accountType)
                {
                    PhoneNumber = i.document.mobilePhone,
                    JobTitle = i.document.jobTitle,
                    OfficeLocation = i.document.officeLocation,
                    PositionInstances = i.document.positions.Select(p => new QueryDepartmentPersonnelPerson.PersonnelPosition
                    {
                        PositionId = p.id,
                        InstanceId = p.instanceId,
                        AppliesFrom = p.appliesFrom!.Value,
                        AppliesTo = p.appliesTo!.Value,
                        Name = p.name,
                        Location = p.locationName,
                        BasePosition = new QueryBasePosition(p.basePosition.id, p.basePosition.name, p.basePosition.discipline, p.basePosition.projectType),
                        Project = new QueryProjectRef(p.project.id, p.project.name, p.project.domainId),
                        Workload = p.workload
                    }).OrderBy(p => p.AppliesFrom).ToList()
                }).ToList();

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

            private IEnumerable<QueryTimelineRange<QueryDepartmentPersonnelPerson.PersonnelTimelineItem>> GenerateTimeline(
                List<QueryDepartmentPersonnelPerson.PersonnelPosition> position,
                List<QueryPersonAbsenceBasic> absences,
                DateTime filterStart,
                DateTime filterEnd)
            {
                // Ensure utc dates
                if (filterStart.Kind != DateTimeKind.Utc)
                    filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

                if (filterEnd.Kind != DateTimeKind.Utc)
                    filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);


                // Gather all dates 
                var dates = position.SelectMany(p => new[] { (DateTime?)p.AppliesFrom.Date, (DateTime?)p.AppliesTo.Date })
                    .Union(absences.SelectMany(a => new[] { a.AppliesFrom.Date, a.AppliesTo?.Date }))
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                if (!dates.Any())
                    yield break;

                var validDates = dates.Where(d => d > filterStart && d < filterEnd).ToList();

                validDates.Insert(0, filterStart);
                validDates.Add(filterEnd);

                var current = validDates.First();
                foreach (var date in validDates.Skip(1))
                {
                    var timelineRange = new TimeRange(current, date);

                    //bool overlap = a.start < b.end && b.start < a.end;

                    var affectedItems = position.Where(p =>
                    {
                        var posTimeRange = new TimeRange(p.AppliesFrom.Date, p.AppliesTo.Date);
                        return posTimeRange.OverlapsWith(timelineRange);
                    });
                    var relevantAbsence = absences.Where(p => p.AppliesTo.HasValue && timelineRange.OverlapsWith(new TimeRange(p.AppliesFrom.Date, p.AppliesTo!.Value.Date)));

                    yield return new QueryTimelineRange<QueryDepartmentPersonnelPerson.PersonnelTimelineItem>(current, date)
                    {
                        Items = affectedItems.Select(p => new QueryDepartmentPersonnelPerson.PersonnelTimelineItem()
                        {
                            Type = "PositionInstance",
                            Workload = p.Workload,
                            Id = p.PositionId,
                            Description = $"{p.Name}",
                            BasePosition = p.BasePosition,
                            Project = p.Project
                        }).Union(relevantAbsence.Select(a => new QueryDepartmentPersonnelPerson.PersonnelTimelineItem()
                        {
                            Id = a.Id,
                            Type = "Absence",
                            Workload = a.AbsencePercentage,
                            Description = $"{a.Type}"
                        }))
                        .ToList(),
                        Workload = affectedItems.Sum(p => p.Workload) + relevantAbsence.Where(a => a.AbsencePercentage.HasValue).Sum(a => a.AbsencePercentage!.Value)
                    };

                    current = date;
                }
            }


            private static Dictionary<string, string> departmentSectors = null!;

            private Dictionary<string, string> LoadSectorInfo()
            {
                if (departmentSectors is null)
                {
                    using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Fusion.Resources.Api.Controllers.Person.departmentSectors.json"))
                    using (var r = new StreamReader(s))
                    {
                        var json = r.ReadToEnd();

                        var sectorInfo = JsonConvert.DeserializeAnonymousType(json, new[] { new { sector = string.Empty, departments = Array.Empty<string>() } });

                        departmentSectors = new Dictionary<string, string>();

                        foreach (var sector in sectorInfo)
                        {
                            sector.departments.ToList().ForEach(d => departmentSectors[d] = sector.sector);
                            departmentSectors[sector.sector] = sector.sector;
                        }
                    }
                }

                return departmentSectors;
            }
        }
    }
}
