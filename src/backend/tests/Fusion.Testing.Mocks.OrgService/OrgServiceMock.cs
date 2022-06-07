using Fusion.ApiClients.Org;
using Fusion.Testing.Mocks.OrgService.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

namespace Fusion.Testing.Mocks.OrgService
{
    public class ApiInvocation
    {
        public HttpMethod Method { get; set; }
        public string Body { get; set; }
        public string Path { get; set; }
        public QueryString Query { get; internal set; }
        public Dictionary<string, string> Headers { get; set; }
    }

    public class OrgServiceMock
    {
        readonly WebApplicationFactory<Startup> factory;

        public static ConcurrentBag<ApiInvocation> Invocations = new ConcurrentBag<ApiInvocation>();

        internal static ConcurrentBag<ApiProjectV2> projects = new();
        internal static ConcurrentBag<ApiPositionV2> positions = new();
        internal static ConcurrentDictionary<Guid, List<ApiProjectContractV2>> contracts = new();
        internal static ConcurrentDictionary<Guid, ApiPositionV2> contractPositions = new();
        internal static ConcurrentBag<ApiCompanyV2> companies = new();

        internal static ConcurrentDictionary<Guid, Guid> taskOwnerMapping = new ConcurrentDictionary<Guid, Guid>();

        public OrgServiceMock()
        {
            factory = new WebApplicationFactory<Startup>();
        }

        public HttpClient CreateHttpClient()
        {
            var client = factory.CreateClient();
            return client;
        }


        public static void AddProject(FusionTestProjectBuilder builder)
        {
            projects.Add(builder.Project);
            foreach (var position in builder.Positions) positions.Add(position);

            foreach ((var contract, var positions) in builder.ContractsWithPositions)
            {
                if (!contracts.ContainsKey(builder.Project.ProjectId))
                    contracts[builder.Project.ProjectId] = new List<ApiProjectContractV2>();

                contracts[builder.Project.ProjectId].Add(contract);

                foreach (var position in positions) contractPositions.TryAdd(position.Id, position);

                if (contract.Company != null && !companies.Any(c => c.Id == contract.Company.Id))
                {
                    companies.Add(contract.Company);
                }
            }
        }
        public static void AddCompany(Guid id, string name)
        {
            companies.Add(new ApiCompanyV2 { Id = id, Name = name });
        }

        public static void SetTaskOwner(Guid position, Guid taskOwnerPosition)
        {
            taskOwnerMapping.TryAdd(position, taskOwnerPosition);
        }

        public static List<ApiPositionV2> GetContractPositions()
        {
            return contractPositions.Select(contractPosition => contractPosition.Value).ToList();
        }

        public static ApiPositionV2 GetPosition(Guid id)
        {
            return positions.FirstOrDefault(p => p.Id == id);
        }
        public static ApiProjectV2 GetProject(Guid id)
        {
            return projects.FirstOrDefault(p => p.ProjectId == id);
        }

        public static void RemoveInstance(Guid id)
        {
            var instance = positions.SelectMany(x => x.Instances).FirstOrDefault(x => x.Id == id);
            if(instance is null) return;
            positions.FirstOrDefault(x => x.Id == instance.PositionId)?.Instances.Remove(instance);
        }
    }
}
