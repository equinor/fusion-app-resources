using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.Http;
using Fusion.Integration.Profile;
using Fusion.Resources.Api.Integrations;
using Fusion.Resources.Domain;
using Itenso.TimePeriod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace Fusion.Resources.Api.Controllers
{
    [ApiVersion("1.0-preview")]
    [Authorize]
    [ApiController]
    public class InternalPersonnelController : ResourceControllerBase
    {
        private readonly IHttpClientFactory httpClientFactory;

        public InternalPersonnelController(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }



        [HttpGet("departments/{fullDepartmentString}/resources/personnel")]
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> GetDepartmentPersonnel(string fullDepartmentString,
            [FromQuery] ODataQueryParams query,
            [FromQuery] DateTime? timelineStart = null,
            [FromQuery] string? timelineDuration = null,
            [FromQuery] DateTime? timelineEnd = null)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    // TODO add
                    // - Resource owner in line org chain (all departments upwrards)
                    // - Is resource owner in general (?)
                    // - Fusion.Resources.Department.ReadAll in any department scope upwards in line org.
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            #endregion

            // Departments are 
            //if (query is null) query = new ODataQueryParams { Top = 1000 };
            //if (query.Top > 1000) return ApiErrors.InvalidPageSize("Max page size is 1000");

            #region Validate input if timeline is expanded

            var shouldExpandTimeline = query.ShoudExpand("timeline");
            if (shouldExpandTimeline)
            {
                if (timelineStart is null)
                    return ApiErrors.MissingInput(nameof(timelineStart), "Must specify 'timelineStart' when expanding timeline");

                TimeSpan? duration;

                try { duration = timelineDuration != null ? XmlConvert.ToTimeSpan("P5M") : null; }
                catch (Exception ex)
                {
                    return ApiErrors.InvalidInput("Invalid duration value: " + ex.Message);
                }

                if (timelineEnd is null)
                {
                    if (duration is null)
                        return ApiErrors.MissingInput(nameof(timelineDuration), "Must specify either 'timelineDuration' or 'timelineEnd' when expanding timeline");

                    timelineEnd = timelineStart.Value.Add(duration.Value);
                }
            }

            #endregion


            var departmentPersonnel = await GetDepartmentPersonnel(fullDepartmentString);

            departmentPersonnel.ForEach(p =>
            {
                var absence = new List<Absence>();
                if (p.Mail == "wkr@equinor.com")
                {
                    absence.Add(new Absence()
                    {
                        Start = new DateTime(2020, 02, 01, 00, 00, 00, DateTimeKind.Utc),
                        End = new DateTime(2020, 02, 14, 00, 00, 00, DateTimeKind.Utc),
                        Id = Guid.NewGuid(),
                        Type = "Vacation"
                    });
                    absence.Add(new Absence()
                    {
                        Start = new DateTime(2018, 02, 21, 00, 00, 00, DateTimeKind.Utc),
                        End = new DateTime(2018, 05, 10, 00, 00, 00, DateTimeKind.Utc),
                        Id = Guid.NewGuid(),
                        AbsencePercentage = 30,
                        Type = "Sick leave"
                    });
                    absence.Add(new Absence()
                    {
                        Start = new DateTime(2020, 01, 01, 00, 00, 00, DateTimeKind.Utc),
                        End = new DateTime(2020, 04, 30, 00, 00, 00, DateTimeKind.Utc),
                        Id = Guid.NewGuid(),
                        AbsencePercentage = 60,
                        Type = "Secret Job"
                    });
                }

                if (shouldExpandTimeline)
                {
                    // Timeline date input has been verified when shouldExpandTimline is true.
                    p.Timeline = GenerateTimeline(p.PositionInstances, absence, timelineStart!.Value, timelineEnd!.Value).OrderBy(p => p.AppliesFrom)
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



                p.EmploymentStatuses = absence.Select(a => new ApiInternalPersonnelPerson.PersonnelAbsence()
                {
                    AbsencePercentage = a.AbsencePercentage,
                    AppliesFrom = a.Start,
                    AppliesTo = a.End,
                    Id = a.Id,
                    Type = a.Type
                }).ToList();
            });

            //var externalPersonell = await DispatchAsync(new GetExternalPersonnel(query));
            //var apiModelItems = externalPersonell.Select(ep => new ApiExternalPersonnelPerson(ep));

            return new ApiCollection<ApiInternalPersonnelPerson>(departmentPersonnel);
        }

       

        [HttpGet("sectors/{sector}/resources/personnel")]
        public async Task<IActionResult> GetSectorPersonnel(string sector)
        {

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    // TODO: Figure out auth requirements
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            return null;
        }


        [HttpGet("sectors/{sectorString}/resources/personnel/timeline")]
        public async Task<IActionResult> GetSectorPersonnelTimeline(string sectorString)
        {

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();

                    or.FullControlInternal();

                    // TODO: Figure out auth requirements
                });
            });

            if (authResult.Unauthorized)
                return authResult.CreateForbiddenResponse();

            // resolve child departments from line org
            // GET https://pro-s-lineorg-ci.azurewebsites.net/lineorg/departments/PRD FE MMS?api-version=1.0&$expand=children

            var sector = await GetSector(sectorString);
            var childDepartments = await FindChildDepartments(sector);

            var personnel = new List<object>();
            foreach(var child in childDepartments)
            {
                personnel.AddRange(await GetDepartmentPersonnel(child.FullName));
            }

            // resolve all persons in all child departments
            // 

            // structure return payload

            return null;
        }

        private async  Task<List<Department>> FindChildDepartments(Department sector)
        {
            if (sector.Children == null || !sector.Children.Any())
                return new List<Department>();

            var lineOrgClient = httpClientFactory.CreateClient("LineOrg");

            var departments = new List<Department>();
            foreach(var child in sector.Children)
            {
                var resource = $"/lineorg/departments/{child.Name}?api-version=1.0&$expand=children";
                var response = await lineOrgClient.GetAsync(resource);
                if (!response.IsSuccessStatusCode)
                {
                    throw new LineOrgIntegrationError();
                }

                var json = await response.Content.ReadAsStringAsync();
                var department = JsonConvert.DeserializeObject<Department>(json);
                departments.Add(department);
                departments.AddRange(await FindChildDepartments(department));
            }
            return departments;
        }

        private async Task<Department> GetSector(string sectorString)
        {
            var lineOrgClient = httpClientFactory.CreateClient("LineOrg");
            var resource = $"/lineorg/departments/{sectorString}?api-version=1.0&$expand=children";

            var response = await lineOrgClient.GetAsync(resource);
            if(!response.IsSuccessStatusCode)
            {
                throw new LineOrgIntegrationError();
            }

            return JsonConvert.DeserializeObject<Department>(await response.Content.ReadAsStringAsync());
        }

        private async Task<List<ApiInternalPersonnelPerson>> GetDepartmentPersonnel(string fullDepartmentString)
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
                                        id = Guid.Empty
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

            var departmentPersonnel = items.results.Select(i => new ApiInternalPersonnelPerson(i.document.azureUniqueId, i.document.mail, i.document.name, i.document.accountType)
            {
                PhoneNumber = i.document.mobilePhone,
                JobTitle = i.document.jobTitle,
                PositionInstances = i.document.positions.Select(p => new ApiInternalPersonnelPerson.PersonnelPosition
                {
                    PositionId = p.id,
                    InstanceId = p.instanceId,
                    AppliesFrom = p.appliesFrom!.Value,
                    AppliesTo = p.appliesTo!.Value,
                    Name = p.name,
                    Location = p.locationName,
                    BasePosition = new ApiBasePosition(p.basePosition.id, p.basePosition.name, p.basePosition.discipline, p.basePosition.projectType),
                    Project = new ApiProjectReference(p.project.id, p.project.name),
                    Workload = p.workload
                }).OrderBy(p => p.AppliesFrom).ToList()
            }).ToList();
            return departmentPersonnel;
        }

        public class Absence
        {
            public Guid Id { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            public double? AbsencePercentage { get; set; }

            public string Type { get; set; } = null!;
        }

        private IEnumerable<ApiInternalPersonnelPerson.TimelineRange> GenerateTimeline(List<ApiInternalPersonnelPerson.PersonnelPosition> position, List<Absence> absences, DateTime filterStart, DateTime filterEnd)
        {
            // Ensure utc dates
            if (filterStart.Kind != DateTimeKind.Utc)
                filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

            if (filterEnd.Kind != DateTimeKind.Utc)
                filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);


            // Gather all dates 
            var dates = position.SelectMany(p => new[] { p.AppliesFrom.Date, p.AppliesTo.Date })
                .Union(absences.SelectMany(a => new[] { a.Start, a.End }))
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
                var relevantAbsence = absences.Where(p => timelineRange.OverlapsWith(new TimeRange(p.Start, p.End)));

                yield return new ApiInternalPersonnelPerson.TimelineRange()
                {
                    AppliesFrom = current,
                    AppliesTo = date,
                    Items = affectedItems.Select(p => new ApiInternalPersonnelPerson.TimelineItem()
                    {
                        Type = "PositionInstance",
                        Workload = p.Workload,
                        Id = p.PositionId,
                        Description = $"{p.Name}",
                        BasePosition = p.BasePosition,
                        Project = p.Project
                    }).Union(relevantAbsence.Select(a => new ApiInternalPersonnelPerson.TimelineItem()
                    {
                        Type = "Absence",
                        Workload = a.AbsencePercentage,
                        Id = a.Id,
                        Description = a.Type
                    }))
                    .ToList(),
                    Workload = affectedItems.Sum(p => p.Workload) + relevantAbsence.Where(a => a.AbsencePercentage.HasValue).Sum(a => a.AbsencePercentage!.Value)
                };

                current = date;
            }
        }
    }
    //    private IEnumerable<ApiInternalPersonnelPerson.TimelineRange> GenerateTimeline(List<ApiInternalPersonnelPerson.PersonnelPosition> position)
    //    {
    //        // Gather all dates 
    //        var dates = position.SelectMany(p => new[] { p.AppliesFrom.Date, p.AppliesTo.Date })
    //            .Distinct()
    //            .OrderBy(d => d)
    //            .ToList();


    //        if (!dates.Any())
    //            yield break;


    //        for (int i = 0; i < dates.Count; i += 2)
    //        {
    //            var start = dates[i];
    //            var end = dates.ElementAtOrDefault(i + 1);

    //            // Odd number of dates. Must use [last period end date + 1 day -> last date] as period
    //            if (end == DateTime.MinValue)
    //            {
    //                start = dates[i - 1].AddDays(1);
    //                end = dates[i];
    //            }



    //            var affectedItems = position.Where(p => p.AppliesFrom < end && start < p.AppliesTo);

    //            yield return new ApiInternalPersonnelPerson.TimelineRange()
    //            {
    //                AppliesFrom = start,
    //                AppliesTo = end,
    //                Items = affectedItems.Select(p => new ApiInternalPersonnelPerson.TimelineItem()
    //                {
    //                    Type = "PositionInstance",
    //                    Workload = p.Workload,
    //                    Id = p.PositionId,
    //                    Description = $"{p.Project.Name}"
    //                }).ToList(),
    //                Workload = affectedItems.Sum(p => p.Workload)
    //            };
    //        }
    //    }
    //}

    public class ApiInternalPersonnelPerson
    {
        public ApiInternalPersonnelPerson(Guid? azureUniqueId, string mail, string name, string accountType)
        {
            AzureUniquePersonId = azureUniqueId;
            Mail = mail;
            Name = name;
            AccountType = accountType;
        }

        public Guid? AzureUniquePersonId { get; set; }
        public string Mail { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? JobTitle { get; set; }

        /// <summary>
        /// Enum, <see cref="FusionAccountType"/>.
        /// </summary>
        public string AccountType { get; set; }


        public List<PersonnelPosition> PositionInstances { get; set; } = new List<PersonnelPosition>();
        public List<PersonnelAbsence> EmploymentStatuses { get; set; } = new List<PersonnelAbsence>();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<TimelineRange> Timeline { get; set; }


        public class TimelineRange
        {
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public List<TimelineItem> Items { get; set; } = new List<TimelineItem>();
            public double Workload { get; set; }
        }

        public class TimelineItem
        {
            public Guid Id { get; set; }
            public string Type { get; set; } = null!;
            public double? Workload { get; set; }
            public string Description { get; set; } = null!;

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ApiProjectReference? Project { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public ApiBasePosition? BasePosition { get; set; }
        }

        public class PersonnelPosition
        {
            public Guid PositionId { get; set; }
            public Guid InstanceId { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }

            public ApiBasePosition BasePosition { get; set; } = null!;
            public string Name { get; set; } = null!;
            public string? Location { get; set; }

            public bool IsActive => AppliesFrom >= DateTime.UtcNow.Date && AppliesTo >= DateTime.UtcNow.Date;
            public double Workload { get; set; }
            public ApiProjectReference Project { get; set; } = null!;
        }
        public class PersonnelAbsence
        {
            public Guid Id { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public double? AbsencePercentage { get; set; }
            public string Type { get; set; } = null!;
        }
    }

}
