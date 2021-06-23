using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Fusion.Testing.Mocks
{
    public class ApiLineOrgManager
    {
        public Guid AzureUniqueId { get; set; }
        public string Mail { get; set; }
        public string Department { get; set; }
        public string FullDepartment { get; set; }
        public string Name { get; set; }
    }
     public class ApiLineOrgUser
    {
        public Guid AzureUniqueId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Guid? ManagerId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ApiLineOrgManager? Manager { get; set; }

        public string Department { get; set; }
        public string FullDepartment { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string JobTitle { get; set; }
        public string Mail { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string OfficeLocation { get; set; }
        public string UserType { get; set; }
        public bool IsResourceOwner { get; set; }
        public bool HasChildPositions { get; set; }
        public bool HasOfficeLicense { get; set; }
        public DateTimeOffset Created { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? Updated { get; set; }

        public DateTimeOffset LastSyncDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<ApiLineOrgUser>? Children { get; set; }
    }
    public class ApiDepartment
    {
        //public ApiDepartment(QueryDepartment queryDepartment)
        //{
        //    Name = queryDepartment.Name;
        //    FullName = queryDepartment.FullName;
        //    Parent = queryDepartment.Parent != null ? new ApiDepartmentRef(queryDepartment.Parent.Name, queryDepartment.Parent.FullName) : null;
        //    Children = queryDepartment.Children?.Select(c => new ApiDepartmentRef(c.Name, c.FullName)).ToList();
        //    Members = queryDepartment.Members?.Select(m => new ApiLineOrgUser(m)).ToList();

        //    if (queryDepartment.Manager != null)
        //        Manager = new ApiLineOrgUser(queryDepartment.Manager);
        //}

        public string Name { get; set; }

        public string FullName { get; set; }

        public ApiDepartmentRef Parent { get; set; }

        public List<ApiDepartmentRef> Children { get; set; }

        public ApiLineOrgUser Manager { get; set; }

        public List<ApiLineOrgUser> Members { get; set; }

        public class ApiDepartmentRef
        {
            public string Name { get; set; }

            public string FullName { get; set; }
        }
    }
      public class ApiPagedCollection<T>
    {
        public ApiPagedCollection(IEnumerable<T> items, int totalCount)
        {
            Value = items;
            TotalCount = totalCount;
            Count = items.Count();
        }

        /// <summary>
        /// Total count without paging applied. This count indicates how many results exists with just using the filters.
        /// </summary>
        public int TotalCount { get; set; }
        public int Count { get; set; }

        /// <summary>
        /// Always show the nextpage, as that is an indication that there are no more pages.
        /// </summary>
        [JsonProperty(PropertyName = "@nextPage", NullValueHandling = NullValueHandling.Include)]
        public string? NextPage { get; set; }

        /// <summary>
        /// Convenience only, hide if not applicable.
        /// </summary>
        [JsonProperty(PropertyName = "@prevPage", NullValueHandling = NullValueHandling.Ignore)]
        public string? PrevPage { get; set; }

        [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<T> Value { get; set; }


        public ApiPagedCollection<T> SetPagingUrls(ODataQueryParams queryParams, HttpRequest request)
        {
            var currentUrl = $"{request.Path}{request.QueryString}";

            var currentSkip = queryParams.Skip.GetValueOrDefault(0);
            var pageSize = queryParams.Top.GetValueOrDefault(Count);

            // Is there a next page?
            var nextSkip = currentSkip + pageSize;
            var prevSkip = Math.Max(currentSkip - pageSize, 0);

            currentUrl = EnsureSkip(currentUrl);

            if (nextSkip < TotalCount)
            {
                var nextUrl = Regex.Replace(currentUrl, @"\$skip=\d+", $"$skip={nextSkip}");
                NextPage = nextUrl;
            }

            if (currentSkip > 0)
            {
                var prevUrl = Regex.Replace(currentUrl, @"\$skip=\d+", $"$skip={prevSkip}");
                PrevPage = prevUrl;
            }

            return this;
        }

        private static string EnsureSkip(string currentUrl)
        {
            // NextPage/PrevPage is dependant on a skip argument to be able to create urls.
            if (Regex.IsMatch(currentUrl, @"\$skip=\d+"))
            {
                return currentUrl;
            }

            var arg = currentUrl.Contains("?") ? "&" : "?";
            currentUrl = currentUrl.TrimEnd('/') + $"{arg}$skip=0";
            return currentUrl;
        }
    }
}
