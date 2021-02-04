using Fusion.AspNetCore.FluentAuthorization;
using Fusion.AspNetCore.OData;
using Fusion.Authorization;
using Fusion.Integration.Http;
using Fusion.Integration.Profile;
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
        public async Task<ActionResult<ApiCollection<ApiInternalPersonnelPerson>>> GetDepartmentPersonnel(string fullDepartmentString, [FromQuery] ODataQueryParams query)
        {
            #region Authorization

            var authResult = await Request.RequireAuthorizationAsync(r =>
            {
                r.AnyOf(or =>
                {
                    or.BeTrustedApplication();
                    or.FullControl();                    
                    
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


            var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);

            var response = await peopleClient.PostAsJsonAsync("/search/persons/query", new
            {
                filter = $"department eq '{fullDepartmentString}'"
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
                            mobilePhone = string.Empty,
                            officeLocation = string.Empty,
                            upn = string.Empty,
                            accountType = string.Empty,
                            isExpired = false,

                            positions = new [] {
                                new {
                                    id = Guid.Empty,
                                    name = string.Empty,
                                    appliesFrom = (DateTime?) null,
                                    appliesTo = (DateTime?) null,
                                    isActive = false,
                                    obs = string.Empty,
                                    locationName = string.Empty,

                                    project = new
                                    {
                                        name = string.Empty,
                                        id = Guid.Empty
                                    },

                                    basePosition = new
                                    {
                                        id = Guid.Empty,
                                        name = string.Empty
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
                    AppliesFrom = p.appliesFrom!.Value,
                    AppliesTo = p.appliesTo!.Value,
                    PositionId = p.id,
                    Project = new ApiProjectReference(p.project.id, p.project.name),
                    Workload = 1.0
                }).OrderBy(p => p.AppliesFrom).ToList()
            }).ToList();

            departmentPersonnel.ForEach(p =>
            {
                var absence = new List<Absence>();
                if (p.Mail == "wkr@equinor.com")
                {
                    absence.Add(new Absence()
                    {
                        Start = new DateTime(2020, 02, 01),
                        End = new DateTime(2020, 02, 14),
                        Id = Guid.NewGuid(),
                        Type = "Vacation"
                    });
                    absence.Add(new Absence()
                    {
                        Start = new DateTime(2018, 02, 21),
                        End = new DateTime(2018, 05, 10),
                        Id = Guid.NewGuid(),
                        AbsencePercentage = 0.3,
                        Type = "Sick leave"
                    });
                    absence.Add(new Absence()
                    {
                        Start = new DateTime(2020, 01, 01),
                        End = new DateTime(2020, 04, 30),
                        Id = Guid.NewGuid(),
                        AbsencePercentage = 0.6,
                        Type = "Secret Job"
                    });
                }
                p.Timeline = GenerateTimeline(p.PositionInstances, absence).OrderBy(p => p.AppliesFrom)
                    .Where(t => (t.AppliesTo - t.AppliesFrom).Days > 2) // We do not wnat 1 day intervals that occur due to from/to do not overlap
                    .ToList();

                // Tweek ranges where end date == next start date
                var indexToMoveBack = new List<int>();
                for (int i = 1; i < p.Timeline.Count; i++)
                {
                    var now = p.Timeline.ElementAt(i);
                    var next = p.Timeline.ElementAtOrDefault(i + 1);

                    if (next != null && now.AppliesTo == next.AppliesFrom)
                        now.AppliesTo = now.AppliesTo.Subtract(TimeSpan.FromDays(1));
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

        public class Absence
        {
            public Guid Id { get; set; }
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            public double? AbsencePercentage { get; set; }

            public string Type { get; set; } = null!;
        }

        private IEnumerable<ApiInternalPersonnelPerson.TimelineRange> GenerateTimeline(List<ApiInternalPersonnelPerson.PersonnelPosition> position, List<Absence> absences)
        {
            // Gather all dates 
            var dates = position.SelectMany(p => new[] { p.AppliesFrom.Date, p.AppliesTo.Date })
                .Union(absences.SelectMany(a => new[] { a.Start, a.End }))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (!dates.Any())
                yield break;

            var filterStart = new DateTime(2020, 01, 01);
            var filterEnd = filterStart.AddMonths(5);

            var validDates = dates.Where(d => d > filterStart && d < filterEnd).ToList();
            if (!validDates.Any())
                yield break;

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
                        Description = $"{p.Project.Name}"
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

        public List<TimelineRange> Timeline { get; set; } = new List<TimelineRange>();


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
        }

        public class PersonnelPosition
        {
            public Guid PositionId { get; set; }
            public Guid InstanceId { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }

            public double Workload { get; set; }
            public ApiProjectReference Project { get; set; } = null!;
        }
        public class PersonnelAbsence
        {
            public Guid Id { get; set; }
            public DateTime AppliesFrom { get; set; }
            public DateTime AppliesTo { get; set; }
            public double? AbsencePercentage { get; set; }
            public string Type { get; set; }
        }
    }

}
