using Fusion.Integration.Http;
using Fusion.Integration.Org;
using MediatR;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Commands
{
    public class SearchPersonnel : IRequest<IEnumerable<QueryInternalPersonnelPerson>>
    {
        private string search;

        public SearchPersonnel(string search)
        {
            this.search = search;
        }

        public string? DepartmentFilter { get; set; }

        public class Handler : IRequestHandler<SearchPersonnel, IEnumerable<QueryInternalPersonnelPerson>>
        {
            private readonly IHttpClientFactory httpClientFactory;
            private readonly IProjectOrgResolver orgResolver;

            public Handler(IHttpClientFactory httpClientFactory, IProjectOrgResolver orgResolver)
            {
                this.httpClientFactory = httpClientFactory;
                this.orgResolver = orgResolver;
            }

            public async Task<IEnumerable<QueryInternalPersonnelPerson>> Handle(SearchPersonnel request, CancellationToken cancellationToken)
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
