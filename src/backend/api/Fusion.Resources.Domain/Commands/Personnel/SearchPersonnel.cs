using Fusion.Integration.Http;
using Fusion.Integration.Org;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public Guid? BasePositionId { get; set; }

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
                if (request.BasePositionId.HasValue)
                {
                    var basePositions = await orgResolver.GetBasePositionsAsync();
                    var bp = basePositions.FirstOrDefault(x => x.Id == request.BasePositionId);
                    if (!string.IsNullOrEmpty(bp?.Department))
                        filter = $"search.ismatch('{bp.Department}','fullDepartment','simple','all')";
                }

                var peopleClient = httpClientFactory.CreateClient(HttpClientNames.ApplicationPeople);
                return await PeopleSearchUtils.GetPersonsFromSearchIndexAsync(peopleClient, request.search, filter);
            }
        }
    }
}
