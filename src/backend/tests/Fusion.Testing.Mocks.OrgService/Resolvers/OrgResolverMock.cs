using Fusion.ApiClients.Org;
using Fusion.Integration.Org;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Testing.Mocks.OrgService.Resolvers
{
    public class OrgResolverMock : Integration.Org.IProjectOrgResolver
    {
        public Task<IEnumerable<ApiBasePositionV2>> GetBasePositionsAsync()
        {
            OrgServiceMock.semaphore.Wait();

            try
            {
                return Task.FromResult(PositionBuilder.AllBasePositions.AsEnumerable());
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }

        public Task<ApiBasePositionV2> ResolveBasePositionAsync(Guid basePositionId)
        {
            OrgServiceMock.semaphore.Wait();

            try
            {
                var bp = PositionBuilder.AllBasePositions.FirstOrDefault(bp => bp.Id == basePositionId);
                return Task.FromResult(bp);
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }

        public Task<ApiBasePositionV2> ResolveBasePositionAsync(string name, OrgProjectType projectType)
        {
            OrgServiceMock.semaphore.Wait();

            try
            {
                var bp = PositionBuilder.AllBasePositions.FirstOrDefault(bp => string.Equals(name, bp.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(projectType.Name, bp.ProjectType));
                return Task.FromResult(bp);
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }

        public Task<ApiProjectContractV2> ResolveContractAsync(Guid projectId, Guid contractId)
        {
            OrgServiceMock.semaphore.Wait();

            try
            {
                var contract = OrgServiceMock.contracts.ContainsKey(projectId) ? OrgServiceMock.contracts[projectId].FirstOrDefault(c => c.Id == contractId) : null;
                return Task.FromResult(contract);
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }

        public Task<ApiPositionV2> ResolvePositionAsync(Guid positionId)
        {
            OrgServiceMock.semaphore.Wait();

            try
            {
                var pos = OrgServiceMock.positions.Union(OrgServiceMock.contractPositions).FirstOrDefault(p => p.Id == positionId);
                return Task.FromResult(pos);
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }

        public Task<IEnumerable<ApiPositionV2>> ResolvePositionsAsync(IEnumerable<Guid> positionIds)
        {
            OrgServiceMock.semaphore.Wait();

            try
            {
                var allPositions = OrgServiceMock.positions.Union(OrgServiceMock.contractPositions).Where(p => positionIds.Contains(p.Id));
                return Task.FromResult(allPositions.ToList().AsEnumerable());
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }

        public Task<ApiProjectV2> ResolveProjectAsync(OrgProjectId projectIdentifier)
        {
            OrgServiceMock.semaphore.Wait();

            try
            {
                var resolvedProject = projectIdentifier.Type switch
                {
                    OrgProjectId.IdentifierType.DomainId => OrgServiceMock.projects.FirstOrDefault(p => string.Equals(p.DomainId, projectIdentifier.DomainId, StringComparison.OrdinalIgnoreCase)),
                    OrgProjectId.IdentifierType.Id => OrgServiceMock.projects.FirstOrDefault(p => p.ProjectId == projectIdentifier.ProjectId),
                    _ => throw new NotImplementedException($"Resolving by type {projectIdentifier.Type} is not implemented in mock")
                };

                return Task.FromResult(resolvedProject);
            }
            finally
            {
                OrgServiceMock.semaphore.Release();
            }
        }
    }
}
