using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public static class PeopleSearchUtils
    {
        /// <summary>
        /// Get department personnel from search index.
        /// </summary>
        /// <param name="peopleClient">HttpClient for Fusion people service</param>
        /// <param name="includeSubDepartments">Certain departments in line org exists where a 
        /// person in the department manages external users. Setting this flag to true will 
        /// include such personnel in the result.</param>
        /// <param name="departments">The deparments to retrieve personnel from.</param>
        /// <returns></returns>
        public static async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(HttpClient peopleClient, bool includeSubDepartments, params string[] departments)
            => await GetDepartmentFromSearchIndexAsync(peopleClient, includeSubDepartments, departments.AsEnumerable());

        /// <summary>
        /// Get department personnel from search index.
        /// </summary>
        /// <param name="peopleClient">HttpClient for Fusion people service</param>
        /// <param name="includeSubDepartments">Certain departments in line org exists where a 
        /// person in the department manages external users. Setting this flag to true will 
        /// include such personnel in the result.</param>
        /// <param name="departments">The deparments to retrieve personnel from.</param>
        /// <returns></returns>
        public static async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(HttpClient peopleClient, bool includeSubDepartments, IEnumerable<string> departments)
        {
            var filterString = string.Join(" or ", departments.Select(dep => $"manager/fullDepartment eq '{dep}'"));

            var searchResponse = await GetFromSearchIndexAsync(peopleClient, 500, filterString, null, includeSubDepartments);
            return searchResponse;
        }

        public static async Task<QueryInternalPersonnelPerson?> GetPersonFromSearchIndexAsync(HttpClient peopleClient, Guid uniqueId)
        {
            var searchResponse = await GetFromSearchIndexAsync(peopleClient, 1, filter: $"azureUniqueId eq '{uniqueId}'");
            return searchResponse.FirstOrDefault();
        }

        public static  async Task<List<QueryInternalPersonnelPerson>> GetPersonsFromSearchIndexAsync(HttpClient peopleClient, string search, string? filter)
        {
            return await GetFromSearchIndexAsync(peopleClient, 500, filter, search);
        }

        private static async Task<List<QueryInternalPersonnelPerson>> GetFromSearchIndexAsync(HttpClient peopleClient, int top, string? filter, string? search = null, bool includeSubDepartments = false)
        {
            var response = await peopleClient.PostAsJsonAsync("/search/persons/query", new
            {
                filter = filter,
                search = search,
                top = top
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
                            managerAzureId = (Guid?)null,

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
                                    allocationUpdated = (DateTime?)null,
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

            var excludedManagers = new HashSet<Guid>();

            if (!includeSubDepartments)
            {
                var uniqueManagers = items.results
                    .Select(x => x.document.managerAzureId)
                    .Distinct()
                    .ToList();

                var subDepartmentManagers = items.results
                    .Where(x => uniqueManagers.Contains(x.document.azureUniqueId))
                    .ToList();

                foreach (var manager in subDepartmentManagers)
                {
                    excludedManagers.Add(manager.document.azureUniqueId);
                }
            }

            var departmentPersonnel = items.results.Select(i => new QueryInternalPersonnelPerson(i.document.azureUniqueId, i.document.mail, i.document.name, i.document.accountType)
            {
                PhoneNumber = i.document.mobilePhone,
                JobTitle = i.document.jobTitle,
                OfficeLocation = i.document.officeLocation,
                Department = i.document.department,
                IsResourceOwner = i.document.isResourceOwner,
                FullDepartment = i.document.fullDepartment,
                ManagerAzureId = i.document.managerAzureId,
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
                    AllocationUpdated = p.allocationUpdated
                }).OrderBy(p => p.AppliesFrom).ToList()
            });

            return departmentPersonnel
                .Where(x => x.ManagerAzureId.HasValue && !excludedManagers.Contains(x.ManagerAzureId.Value))
                .ToList();
        }

    }
}
