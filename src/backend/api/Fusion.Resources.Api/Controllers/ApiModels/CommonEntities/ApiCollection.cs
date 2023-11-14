using Fusion.AspNetCore.OData;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fusion.Resources.Api.Controllers
{
    public static class ApiCollection
    {
        public static ApiCollection<TItem> FromCollection<TItem>(IEnumerable<TItem> items, int? totalCount)
        {
            var apiModel = new ApiCollection<TItem>(items, totalCount ?? items.Count());
            return apiModel;
        }
    }

    public class ApiCollection<T>
    {

        public ApiCollection(IEnumerable<T> items)
        {
            Value = items;
        }

        public ApiCollection(IEnumerable<T> items, int totalCount)
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


        public ApiCollection<T> SetPagingUrls(ODataQueryParams queryParams, HttpRequest request)
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
