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

        internal static List<ApiClients.Org.ApiProjectV2> projects = new List<ApiClients.Org.ApiProjectV2>();
        internal static List<ApiClients.Org.ApiPositionV2> positions = new List<ApiClients.Org.ApiPositionV2>();
        internal static Dictionary<Guid, List<ApiClients.Org.ApiProjectContractV2>> contracts = new Dictionary<Guid, List<ApiClients.Org.ApiProjectContractV2>>();
        internal static List<ApiClients.Org.ApiPositionV2> contractPositions = new List<ApiClients.Org.ApiPositionV2>();
        internal static List<ApiCompanyV2> companies = new List<ApiCompanyV2>();

        internal static ConcurrentDictionary<Guid, Guid> taskOwnerMapping = new ConcurrentDictionary<Guid, Guid>();

        internal static SemaphoreSlim semaphore = new SemaphoreSlim(1);

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
            semaphore.Wait();

            try
            {
                projects.Add(builder.Project);
                positions.AddRange(builder.Positions);

                foreach ((var contract, var positions) in builder.ContractsWithPositions)
                {
                    if (!contracts.ContainsKey(builder.Project.ProjectId))
                        contracts[builder.Project.ProjectId] = new List<ApiProjectContractV2>();

                    contracts[builder.Project.ProjectId].Add(contract);
                    contractPositions.AddRange(positions);

                    if (contract.Company != null && !companies.Any(c => c.Id == contract.Company.Id))
                    {
                        companies.Add(contract.Company);
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
        public static void AddCompany(Guid id, string name)
        {
            semaphore.Wait();
            try
            {
                companies.Add(new ApiCompanyV2 { Id = id, Name = name });
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static void SetTaskOwner(Guid position, Guid taskOwnerPosition)
        {
            taskOwnerMapping.TryAdd(position, taskOwnerPosition);
        }

        public static ApiPositionV2 GetPosition(Guid id)
        {
            return positions.FirstOrDefault(p => p.Id == id);
        }
        public static ApiProjectV2 GetProject(Guid id)
        {
            return projects.FirstOrDefault(p => p.ProjectId == id);
        }
        //public static ApiPositionV2 ResolveContractPosition(Guid position)
        //{

        //}
    }
}
