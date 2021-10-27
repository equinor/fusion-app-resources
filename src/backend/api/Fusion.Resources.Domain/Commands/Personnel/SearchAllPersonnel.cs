using Fusion.Integration.Http;
using MediatR;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    /// <summary>
    /// Search for persons and return all results without paging.
    /// Note: This can cause stress on the search index and produce large result set.
    /// </summary>
    [Obsolete("Use SearchPersonnel instead to get paged result. Use only if required.")]
    public class SearchAllPersonnel : IRequest<IEnumerable<QueryInternalPersonnelPerson>>
    {
        private string search;

        public SearchAllPersonnel(string search)
        {
            this.search = search;
        }

        public string? DepartmentFilter { get; private set; }

        public SearchAllPersonnel WithDepartmentFilter(string? departmentFilter)
        {
            DepartmentFilter = departmentFilter;
            return this;
        }

        public class Handler : IRequestHandler<SearchAllPersonnel, IEnumerable<QueryInternalPersonnelPerson>>
        {
            private readonly IHttpClientFactory httpClientFactory;

            public Handler(IHttpClientFactory httpClientFactory)
            {
                this.httpClientFactory = httpClientFactory;
            }

            public async Task<IEnumerable<QueryInternalPersonnelPerson>> Handle(SearchAllPersonnel request, CancellationToken cancellationToken)
            {
                string? filter = null;
                if (request.DepartmentFilter is not null)
                {
                    filter = $"search.ismatch('{request.DepartmentFilter}','fullDepartment','simple','all')";
                }

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);
                return await PeopleSearchUtils.GetPersonsFromSearchIndexAsync(peopleClient, request.search, filter);
            }
        }
    }

}
