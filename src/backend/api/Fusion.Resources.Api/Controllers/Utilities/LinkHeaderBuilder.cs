using System.Collections.Generic;
using Fusion.AspNetCore.OData;
using Fusion.Resources.Domain;
using Microsoft.AspNetCore.Http;

namespace Fusion.Resources.Api.Controllers
{
    public sealed class LinkHeaderBuilder
    {
        private const string QUERY_URL_FORMATTED = "<{0}?$search={1}&$filter={2}&skip={3}&$top={4}>;";
        private const string URL_FORMATTED = "<{0}?$skip={1}>;";
        private const string REL_FORMATTED = " rel=\"{0}\"";

        public LinkHeaderBuilder()
        {
            Labels = new LinkHeaderLabels();
        }

        public bool UsingQuery { get; private set; }

        public LinkHeaderLabels Labels { get; }

        public int QueryTop { get; private set; }

        public string? QuerySearch { get; private set; }

        public string? QueryFilter { get; private set; }

        public LinkHeaderBuilder WithQuery(HttpRequest httpRequest, ODataQueryParams query)
        {
            UsingQuery = true;

            if (query.HasSearch)
                QuerySearch = query.Search;

            if (query.HasFilter)
                QueryFilter = httpRequest.Query["$filter"].ToString();

            if (query.Top.HasValue)
                QueryTop = query.Top.Value;

            return this;
        }

        /// <summary>
        ///     Build the link header based on the information provided by <paramref name="pagedResult" />
        /// </summary>
        /// <typeparam name="T">Type of the entities in the result</typeparam>
        /// <param name="pagedResult">Contains the page information</param>
        /// <param name="currentRoute">Resource route (URL) where paging information will be appended</param>
        /// <returns>
        ///     A string that is used as the value for the link header.
        /// </returns>
        public string GetLinkHeader<T>(QueryPagedList<T> pagedResult, string currentRoute)
        {
            List<string> links = new();

            AddLinks(links, pagedResult, currentRoute);

            return string.Join(",", links.ToArray());
        }

        /// <summary>
        ///     Add pagination links
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="links">A list of strings each representing a value in the Link Header</param>
        /// <param name="pagedResult">The <see cref="QueryPagedList{T}" /> returned in the response to the client </param>
        /// <param name="currentRoute">The URL for the resource </param>
        /// <remarks>Override this method to add additional link headers.</remarks>
        private void AddLinks<T>
        (
            ICollection<string> links,
            QueryPagedList<T> pagedResult,
            string currentRoute
        )
        {
            if (Next(pagedResult, currentRoute, out var nextLink)) links.Add(nextLink!);

            if (Last(pagedResult, currentRoute, out var lastLink)) links.Add(lastLink!);

            if (First(pagedResult, currentRoute, out var firstLink)) links.Add(firstLink!);

            if (Previous(pagedResult, currentRoute, out var prevLink)) links.Add(prevLink!);
        }

        /// <summary>
        ///     Returns true if the <paramref name="pagedResult" /> has a previous page and outputs the link.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pagedResult">The <see cref="QueryPagedList{T}" /> returned in the response to the client </param>
        /// <param name="currentRoute">The URL for the resource </param>
        /// <param name="prevLink">The URL to the previous page</param>
        /// <returns>true if previous page exists; false, otherwise.</returns>
        private bool Previous<T>(QueryPagedList<T> pagedResult, string currentRoute, out string? prevLink)
        {
            prevLink = null;

            if (pagedResult.CurrentPage > 1)
            {
                if (UsingQuery)
                    prevLink = string.Format(QUERY_URL_FORMATTED, currentRoute, QuerySearch, QueryFilter,
                                   pagedResult.CurrentPage - QueryTop, QueryTop) +
                               string.Format(REL_FORMATTED, Labels.Previous);

                else
                    prevLink = string.Format(URL_FORMATTED, currentRoute, pagedResult.CurrentPage - 1) +
                               string.Format(REL_FORMATTED, Labels.Previous);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if the <paramref name="pagedResult" /> has a first page and outputs the link.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pagedResult">The <see cref="QueryPagedList{T}" /> returned in the response to the client </param>
        /// <param name="currentRoute">The URL for the resource </param>
        /// <param name="firstLink">The URL to the first page</param>
        /// <returns>true if first page exists; false, otherwise.</returns>
        private bool First<T>(QueryPagedList<T> pagedResult, string currentRoute, out string? firstLink)
        {
            firstLink = null;

            if (pagedResult.TotalCount > 0 && pagedResult.PageSize > 0 && pagedResult.CurrentPage != 1)
            {
                if (UsingQuery)
                    firstLink =
                        string.Format(QUERY_URL_FORMATTED, currentRoute, QuerySearch, QueryFilter, 0, QueryTop) +
                        string.Format(REL_FORMATTED, Labels.First);
                else
                    firstLink = string.Format(URL_FORMATTED, currentRoute, 1) +
                                string.Format(REL_FORMATTED, Labels.First);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if the <paramref name="pagedResult" /> has a last page and outputs the link.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pagedResult">The <see cref="QueryPagedList{T}" /> returned in the response to the client </param>
        /// <param name="currentRoute">The URL for the resource </param>
        /// <param name="lastLink">The URL to the last page</param>
        /// <returns>true if last page exists; false, otherwise.</returns>
        private bool Last<T>(QueryPagedList<T> pagedResult, string currentRoute, out string? lastLink)
        {
            lastLink = null;

            if (pagedResult.CurrentPage < pagedResult.TotalPages)
            {
                if (UsingQuery)
                    lastLink = string.Format(QUERY_URL_FORMATTED, currentRoute, QuerySearch, QueryFilter,
                                   pagedResult.TotalCount - QueryTop, QueryTop) +
                               string.Format(REL_FORMATTED, Labels.Last);
                else

                    lastLink = string.Format(URL_FORMATTED, currentRoute, pagedResult.TotalPages) +
                               string.Format(REL_FORMATTED, Labels.Last);

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Returns true if the <paramref name="pagedResult" /> has a next page and outputs the link.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pagedResult">The <see cref="QueryPagedList{T}" /> returned in the response to the client </param>
        /// <param name="currentRoute">The URL for the resource </param>
        /// <param name="nextLink">The URL to the next page</param>
        /// <returns>true if next page exists; false, otherwise.</returns>
        private bool Next<T>(QueryPagedList<T> pagedResult, string currentRoute, out string? nextLink)
        {
            nextLink = null;

            var nextPage = pagedResult.CurrentPage + 1;

            if (nextPage <= pagedResult.TotalPages)
            {
                nextLink = GetUrl(currentRoute, Labels.Next, nextPage);

                return true;
            }

            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="currentRoute"></param>
        /// <param name="label"></param>
        /// <param name="nextPage"></param>
        /// <returns></returns>
        private string GetUrl(string currentRoute, string label, int nextPage)
        {
            if (UsingQuery)
                return string.Format(QUERY_URL_FORMATTED, currentRoute, QuerySearch, QueryFilter, nextPage + QueryTop,
                           QueryTop) +
                       string.Format(REL_FORMATTED, label);
            return string.Format(URL_FORMATTED, currentRoute, nextPage) +
                   string.Format(REL_FORMATTED, label);
        }
    }


    public class LinkHeaderLabels
    {
        public LinkHeaderLabels()
        {
            First = "first";
            Next = "next";
            Previous = "prev";
            Last = "last";
        }

        public string First { get; set; }
        public string Next { get; set; }
        public string Previous { get; set; }
        public string Last { get; set; }
    }
}