using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public static class PeopleSearchUtils
    {
        public static async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(HttpClient peopleClient, params string[] departments) 
            => await GetDepartmentFromSearchIndexAsync(peopleClient, departments.AsEnumerable());
        public static async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(HttpClient peopleClient, IEnumerable<string> departments)
        {
            var response = await peopleClient.PostAsJsonAsync("/search/persons/query", new
            {
                filter = string.Join(" or ", departments.Select(dep => $"manager/fullDepartment eq '{dep}'")),
                top = 500
            });

            var data = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

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
                            isResourceOwner = false,

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
                                    allocationState = string.Empty,
                                    allicationUpdated = (DateTime?)null,
                                    project = new
                                    {
                                        name = string.Empty,
                                        id = Guid.Empty,
                                        domainId = string.Empty,
                                        type = string.Empty
                                    },

                                    basePosition = new
                                    {
                                        id = Guid.Empty,
                                        name = string.Empty,
                                        discipline = string.Empty,
                                        type = string.Empty
                                    }
                                }

                            }
                        }
                    }
                }
            });

            var departmentPersonnel = items.results.Select(i => new QueryInternalPersonnelPerson(i.document.azureUniqueId, i.document.mail, i.document.name, i.document.accountType)
            {
                PhoneNumber = i.document.mobilePhone,
                JobTitle = i.document.jobTitle,
                OfficeLocation = i.document.officeLocation,
                Department = i.document.department,
                IsResourceOwner = i.document.isResourceOwner,
                FullDepartment = departments.Where(d => d.EndsWith(i.document.department)).FirstOrDefault(),
                PositionInstances = i.document.positions.Select(p => new QueryPersonnelPosition
                {
                    PositionId = p.id,
                    InstanceId = p.instanceId,
                    AppliesFrom = p.appliesFrom!.Value,
                    AppliesTo = p.appliesTo!.Value,
                    Name = p.name,
                    Location = p.locationName,
                    BasePosition = new QueryBasePosition(p.basePosition.id, p.basePosition.name, p.basePosition.discipline, p.basePosition.type),
                    Project = new QueryProjectRef(p.project.id, p.project.name, p.project.domainId, p.project.type),
                    Workload = p.workload,
                    AllocationState = p.allocationState,
                    AllocationUpdated = p.allicationUpdated
                }).OrderBy(p => p.AppliesFrom).ToList()
            }).ToList();

            return departmentPersonnel;
        }
    }
}
