using Fusion.ApiClients.Org;
using Fusion.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


#nullable enable
namespace Fusion.Resources.Domain.Services
{
    internal class ProjectOrgResolver : IProjectOrgResolver
    {
        private readonly IOrgApiClient client;
        private readonly ILogger<ProjectOrgResolver> logger;

        public ProjectOrgResolver(ILogger<ProjectOrgResolver> logger, IOrgApiClientFactory orgApiClientFactory)
        {
            client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            this.logger = logger;
        }


        private static AsyncLazy<IEnumerable<ApiBasePositionV2>>? fetchBasePositions = null;
        private Task<IEnumerable<ApiBasePositionV2>> BasePositions 
        { 
            get
            {
                if (fetchBasePositions != null)
                    return fetchBasePositions.Value;

                fetchBasePositions = new AsyncLazy<IEnumerable<ApiBasePositionV2>>(async () =>
                {
                    var positions = await client.GetBasePositionsV2Async(null);
                    return positions;
                });

                return fetchBasePositions.Value;
            } 
        }

        public async Task<ApiBasePositionV2?> ResolveBasePositionAsync(Guid basePositionId)
        {
            var basePositions = await BasePositions;
            return basePositions.FirstOrDefault(bp => bp.Id == basePositionId);
        }

        public async Task<ApiProjectContractV2?> ResolveContractAsync(Guid projectId, Guid contractId)
        {
            try
            {
                var contract = await client.GetContractV2Async(projectId, contractId);
                return contract;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Unable to resolve contract by id for project '{projectId}' and contract id '{contractId}'. Message: {ex.Message}");
                return null;
            }
            
        }
    }
}
