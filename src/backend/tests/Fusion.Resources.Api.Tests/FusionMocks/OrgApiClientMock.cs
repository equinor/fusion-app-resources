using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fusion.ApiClients.Org;
using PersonIdentifier = Fusion.ApiClients.Org.PersonIdentifier;

namespace Fusion.Resources.Api.Tests.FusionMocks
{
    internal class OrgApiClientFactoryMock : IOrgApiClientFactory
    {
        public IOrgApiClient CreateClient(ApiClientMode mode)
        {
            return new OrgApiClientMock();
        }
    }

    internal class OrgApiClientMock : IOrgApiClient
    {
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
        {
            string url = message.RequestUri!.ToString();

            if (Regex.IsMatch(url, "/projects/([^/?]+)/positions"))
            {
                if (message.Method == HttpMethod.Post)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = message, Content = message.Content! };
                }
            }


            throw new NotImplementedException($"Endpoint {message.RequestUri} is not implemented in mock");
        }

        public async Task<List<ApiProjectV2>> GetProjectsV2Async(ODataQuery query = null)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiProjectV2> GetProjectOrDefaultV2Async(OrgProjectId projectId, ODataQuery query = null)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ApiPositionV2>> GetProjectPositionsV2Async(OrgProjectId projectId, ODataQuery query = null)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiPositionV2> GetPositionV2Async(OrgProjectId projectId, Guid positionId, ODataQuery query = null)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiPositionV2> GetPositionV2Async(Guid positionId, ODataQuery query = null)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiPositionV2> CreateContractPositionV2Async(Guid projectId, Guid contractId, ApiPositionV2 position)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiPositionV2> CreatePositionV2Async(Guid projectId, ApiPositionV2 position)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiPositionV2> UpdatePositionV2Async(ApiPositionV2 position)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ApiPositionV2>> GetPersonPositionsV2Async(PersonIdentifier person, ODataQuery query = null)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ApiBasePositionV2>> GetBasePositionsV2Async(ODataQuery query)
        {
            throw new NotImplementedException();
        }

        public async Task DeletePositionV2Async(ApiPositionV2 position, bool force = false)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiProjectContractV2> GetContractV2Async(OrgProjectId projectId, Guid contractId)
        {
            throw new NotImplementedException();
        }

        public async Task<ICollection<ApiProjectContractV2>> GetContractsV2Async(OrgProjectId projectId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ApiPositionV2>> GetContractPositionsV2Async(OrgProjectId projectId, Guid contractId, ODataQuery query = null)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiProjectContractV2> CreateContractV2Async(OrgProjectId projectId, ApiProjectContractV2 contract)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiProjectContractV2> UpdateContractV2Async(OrgProjectId projectId, ApiProjectContractV2 contract)
        {
            throw new NotImplementedException();
        }

        public ApiClientMode ClientMode { get; }
    }
}