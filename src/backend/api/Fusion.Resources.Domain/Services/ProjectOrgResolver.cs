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

        public async Task<ApiPositionV2?> ResolvePositionAsync(Guid positionId)
        {
            try
            {
                var contract = await client.GetPositionV2Async(positionId);
                return contract;
            }
            catch (OrgApiError ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning($"Could not locate position with id {positionId}, received NotFound from server");
                return null;
            }
            catch (OrgApiError ex)
            {
                logger.LogError(ex, $"Error resolving position from org service. Received code {ex.HttpStatusCode} from service, with message: {ex.Message}.");
                logger.LogError(ex.ResponseText);

                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error trying to resolve position id '{positionId}'. Message: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<ApiPositionV2>> ResolvePositionsAsync(IEnumerable<Guid> positionIds)
        {
            var ids = positionIds.ToList();

            var resolvedPositions = new List<ApiPositionV2>();

            int index = 0;
            while (true)
            {
                var page = ids.Skip(index).Take(10);
                index += 10;

                if (page.Count() == 0)
                    break;

                var positions = await Task.WhenAll(page.Select(async id =>
                {
                    try { return await ResolvePositionAsync(id); } catch { return null; }
                }));
                resolvedPositions.AddRange(positions);
            }

            return resolvedPositions;
        }
    }
}
