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
        const int default_page_size = 500;
        /// <summary>
        /// Get department personnel from search index.
        /// </summary>
        /// <param name="peopleClient">HttpClient for Fusion people service</param>
        /// <param name="includeSubDepartments">Certain departments in line org exists where a 
        /// person in the department manages external users. Setting this flag to true will 
        /// include such personnel in the result.</param>
        /// <param name="departments">The deparments to retrieve personnel from.</param>
        /// <returns></returns>
        public static async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(HttpClient peopleClient, params string[] departments)
            => await GetDepartmentFromSearchIndexAsync(peopleClient, departments.AsEnumerable());

        /// <summary>
        /// Get department personnel from search index.
        /// </summary>
        /// <param name="peopleClient">HttpClient for Fusion people service</param>
        /// <param name="includeSubDepartments">Certain departments in line org exists where a 
        /// person in the department manages external users. Setting this flag to true will 
        /// include such personnel in the result.</param>
        /// <param name="departments">The deparments to retrieve personnel from.</param>
        /// <returns></returns>
        public static async Task<List<QueryInternalPersonnelPerson>> GetDepartmentFromSearchIndexAsync(HttpClient peopleClient, IEnumerable<string> departments)
        {
            var filterString = string.Join(" or ", departments.Select(dep => $"manager/fullDepartment eq '{dep}'"));

            var searchResponse = await GetFromSearchIndexAsync(peopleClient, filterString, null);
            return searchResponse;
        }

        public static async Task<List<QueryInternalPersonnelPerson>> GetDirectReportsTo(HttpClient peopleClient, Guid azureUniqueId)
            => await GetFromSearchIndexAsync(peopleClient, $"managerAzureId eq '{azureUniqueId}'");

        public static async Task<QueryInternalPersonnelPerson?> GetPersonFromSearchIndexAsync(HttpClient peopleClient, Guid uniqueId)
        {
            var searchResponse = await GetFromSearchIndexAsync(peopleClient, filter: $"azureUniqueId eq '{uniqueId}'");
            return searchResponse.FirstOrDefault();
        }

        public static async Task<List<QueryInternalPersonnelPerson>> GetPersonsFromSearchIndexAsync(HttpClient peopleClient, string search, string? filter)
        {
            return await GetFromSearchIndexAsync(peopleClient, filter, search);
        }

        private static async Task<List<QueryInternalPersonnelPerson>> GetFromSearchIndexAsync(HttpClient peopleClient, string? filter, string? search = null)
        {
            var result = new List<QueryInternalPersonnelPerson>();

            var top = default_page_size;
            var skip = 0;
            var totalCount = 0;

            do
            {
                var response = await peopleClient.PostAsJsonAsync("/search/persons/query", new { filter, search, top, skip, includeTotalResultCount = true });

                var data = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                var items = JsonConvert.DeserializeAnonymousType(data, new
                {
                    results = new[]
                    {
                        new { document = new SearchPersonDTO() }
                    },
                    count = (int?)0
                });

                skip += items.results.Length;
                totalCount = items.count ?? 0;

                result.AddRange(
                    items.results.Select(i => new QueryInternalPersonnelPerson(i.document.azureUniqueId, i.document.mail, i.document.name, i.document.accountType)
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
                    })
                );
            } while (skip < totalCount);

            return result;
        }

        

        private class SearchProjectDTO
        {
            public string name { get; set; } = null!;
            public Guid id { get; set; }
            public string domainId { get; set; } = null!;
            public string type { get; set; } = null!;
        }
        private class SearchBasePositionDTO
        {
            public Guid id { get; set; }
            public string name { get; set; } = null!;
            public string discipline { get; set; } = null!;
            public string type { get; set; } = null!;
        }

        private class SearchPositionDTO
        {
            public Guid id { get; set; }
            public Guid instanceId { get; set; }
            public string name { get; set; } = null!;
            public DateTime? appliesFrom { get; set; }
            public DateTime? appliesTo { get; set; }
            public bool isActive { get; set; }
            public string? obs { get; set; }
            public string? locationName { get; set; }
            public double workload { get; set; }
            public string allocationState { get; set; } = null!;
            public DateTime? allocationUpdated { get; set; }
            public SearchProjectDTO project { get; set; } = new();
            public SearchBasePositionDTO basePosition { get; set; } = new();
        }
        private class SearchPersonDTO
        {
            public Guid azureUniqueId { get; set; }
            public string mail { get; set; } = null!;
            public string name { get; set; } = null!;
            public string jobTitle { get; set; } = null!;
            public string department { get; set; } = null!;
            public string fullDepartment { get; set; } = null!;
            public string mobilePhone { get; set; } = null!;
            public string officeLocation { get; set; } = null!;
            public string upn { get; set; } = null!;
            public string accountType { get; set; } = null!;
            public bool isExpired { get; set; }
            public bool isResourceOwner { get; set; }
            public Guid? managerAzureId { get; set; }
            public List<SearchPositionDTO> positions { get; set; } = new();

        }
    }
}
