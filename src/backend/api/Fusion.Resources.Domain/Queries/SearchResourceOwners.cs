using Fusion.Integration.Profile.ApiClient;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain.Queries
{
    public class SearchResourceOwners : IRequest<List<ApiPersonProfileV2>>
    {
        public SearchResourceOwners(string query)
        {
            Query = query;
        }

        public string Query { get; }

        public class Handler : IRequestHandler<SearchResourceOwners, List<ApiPersonProfileV2>>
        {
            public async Task<List<ApiPersonProfileV2>> Handle(SearchResourceOwners request, CancellationToken cancellationToken)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
