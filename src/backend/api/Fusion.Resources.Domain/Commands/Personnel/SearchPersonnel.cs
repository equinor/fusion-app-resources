using Fusion.AspNetCore.OData;
using Fusion.Integration.Http;
using MediatR;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class SearchPersonnel : IRequest<QueryRangedList<QueryInternalPersonnelPerson>>
    {

        public SearchPersonnel(string search)
        {
            Search = search;
        }

        public string? DepartmentFilter { get; private set; }
        public string Search { get; private set; }
        public int? Skip { get; set; }
        public int? Top { get; set; }

        public SearchPersonnel WithDepartmentFilter(string? departmentFilter)
        {
            DepartmentFilter = departmentFilter;
            return this;
        }

        /// <summary>
        /// Sets skip and top token from the query params.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public SearchPersonnel WithPaging(ODataQueryParams query)
        {
            Skip = query.Skip;
            Top = query.Top;

            return this;
        }

        public class Handler : IRequestHandler<SearchPersonnel, QueryRangedList<QueryInternalPersonnelPerson>>
        {
            private readonly IHttpClientFactory httpClientFactory;

            public Handler(IHttpClientFactory httpClientFactory)
            {
                this.httpClientFactory = httpClientFactory;
            }

            public async Task<QueryRangedList<QueryInternalPersonnelPerson>> Handle(SearchPersonnel request, CancellationToken cancellationToken)
            {
                string? filter = null;
                if (request.DepartmentFilter is not null)
                {
                    filter = $"search.ismatch('{request.DepartmentFilter}','fullDepartment','simple','all')";
                }

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);
                var items = await PeopleSearchUtils.Search(peopleClient, request.Search, s => s
                    .WithFilter(filter)
                    .WithSkip(request.Skip)
                    .WithTop(request.Top));

                return items;
            }
        }
    }

}
