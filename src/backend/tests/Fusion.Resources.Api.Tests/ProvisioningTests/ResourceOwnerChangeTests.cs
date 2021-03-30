using Fusion.ApiClients.Org;
using Fusion.Resources.Database;
using Fusion.Resources.Logic.Commands;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Api.Tests.ProvisioningTests
{
    public class ResourceOwnerProvisioningTests
    {

        internal class FakeOrgClient : IOrgApiClient
        {
            public ApiClientMode ClientMode { get; set; }

            public Task<HttpResponseMessage> SendAsync(HttpRequestMessage message)
            {
                throw new NotImplementedException();
            }

            #region Not implemented

            public Task<ApiPositionV2> CreateContractPositionV2Async(Guid projectId, Guid contractId, ApiPositionV2 position)
            {
                throw new NotImplementedException();
            }

            public Task<ApiProjectContractV2> CreateContractV2Async(OrgProjectId projectId, ApiProjectContractV2 contract)
            {
                throw new NotImplementedException();
            }

            public Task<ApiPositionV2> CreatePositionV2Async(Guid projectId, ApiPositionV2 position)
            {
                throw new NotImplementedException();
            }

            public Task DeletePositionV2Async(ApiPositionV2 position, bool force = false)
            {
                throw new NotImplementedException();
            }

            public Task<List<ApiBasePositionV2>> GetBasePositionsV2Async(ODataQuery query)
            {
                throw new NotImplementedException();
            }

            public Task<List<ApiPositionV2>> GetContractPositionsV2Async(OrgProjectId projectId, Guid contractId, ODataQuery query = null)
            {
                throw new NotImplementedException();
            }

            public Task<ICollection<ApiProjectContractV2>> GetContractsV2Async(OrgProjectId projectId)
            {
                throw new NotImplementedException();
            }

            public Task<ApiProjectContractV2> GetContractV2Async(OrgProjectId projectId, Guid contractId)
            {
                throw new NotImplementedException();
            }

            public Task<List<ApiPositionV2>> GetPersonPositionsV2Async(PersonIdentifier person, ODataQuery query = null)
            {
                throw new NotImplementedException();
            }

            public Task<ApiPositionV2> GetPositionTaskOwnerV2Async(OrgProjectId projectId, Guid positionId, DateTime? date)
            {
                throw new NotImplementedException();
            }

            public Task<ApiPositionV2> GetPositionV2Async(OrgProjectId projectId, Guid positionId, ODataQuery query = null)
            {
                throw new NotImplementedException();
            }

            public Task<ApiPositionV2> GetPositionV2Async(Guid positionId, ODataQuery query = null)
            {
                throw new NotImplementedException();
            }

            public Task<ApiProjectV2> GetProjectOrDefaultV2Async(OrgProjectId projectId, ODataQuery query = null)
            {
                throw new NotImplementedException();
            }

            public Task<List<ApiPositionV2>> GetProjectPositionsV2Async(OrgProjectId projectId, ODataQuery query = null)
            {
                throw new NotImplementedException();
            }

            public Task<List<ApiProjectV2>> GetProjectsV2Async(ODataQuery query = null)
            {
                throw new NotImplementedException();
            }

            public Task<ApiProjectContractV2> UpdateContractV2Async(OrgProjectId projectId, ApiProjectContractV2 contract)
            {
                throw new NotImplementedException();
            }

            public Task<ApiPositionV2> UpdatePositionV2Async(ApiPositionV2 position)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        [Fact]
        public async Task Test()
        {
            var testPerson = new FusionTestUserBuilder().SaveProfile();
            ApiPositionInstanceV2 testInstance = null!;

            var testPosition = PositionBuilder.NewPosition()
                .WithInstances(s =>
                {
                    testInstance = s.AddInstance(DateTime.Today.AddDays(-100), TimeSpan.FromDays(200))
                        .SetAssignedPerson(testPerson);
                });

            var request = new Database.Entities.DbResourceAllocationRequest()
            {
                Id = Guid.NewGuid(),
                Type = Database.Entities.DbInternalRequestType.ResourceOwnerChange,
                SubType = "removeResource",
                ProposalParameters = new Database.Entities.DbResourceAllocationRequest.DbOpProposalParameters()
                {
                    ChangeFrom = DateTime.UtcNow.AddDays(10)
                },
                OrgPositionId = testPosition.Id,
                OrgPositionInstance = new Database.Entities.DbResourceAllocationRequest.DbOpPositionInstance
                {
                    AppliesFrom = testInstance.AppliesFrom,
                    AppliesTo = testInstance.AppliesTo,
                    AssignedToMail = testInstance.AssignedPerson?.Mail,
                    AssignedToUniqueId = testInstance.AssignedPerson?.AzureUniqueId,
                    Id = testInstance.Id,
                    Obs = testInstance.Obs,
                    Workload = testInstance.Workload
                },
                Project = new Database.Entities.DbProject
                {
                    OrgProjectId = Guid.Empty,
                    Name = "Test project"
                }
            };

            var orgClientMock = new Mock<IOrgApiClient>();
            orgClientMock.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get))).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(testPosition), Encoding.UTF8, "application/json")
            });
            orgClientMock.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Put))).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK
                //Content =  nunew StringContent(JsonConvert.SerializeObject(testPosition), Encoding.UTF8, "application/json")
            });

            var factoryMock = new Mock<IOrgApiClientFactory>();
            factoryMock.Setup(c => c.CreateClient(ApiClientMode.Application)).Returns(orgClientMock.Object);


            var options = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase($"{Guid.NewGuid()}")
                .Options;

            using var dbContext = new ResourcesDbContext(options);
            dbContext.ResourceAllocationRequests.Add(request);
            dbContext.SaveChanges();


            var cmd = new ResourceAllocationRequest.ResourceOwner.ProvisionResourceOwnerRequest(request.Id);
            var handler = new ResourceAllocationRequest.ResourceOwner.ProvisionResourceOwnerRequest.Handler(dbContext, factoryMock.Object)
                as IRequestHandler<ResourceAllocationRequest.ResourceOwner.ProvisionResourceOwnerRequest>;
            await handler.Handle(cmd, CancellationToken.None);


            var i = orgClientMock.Invocations.SelectMany(i => i.Arguments.Cast<HttpRequestMessage>())
                .FirstOrDefault(m => m.Method == HttpMethod.Put);
            var postedPositionJson = await i.Content.ReadAsStringAsync();
            var postedPosition = JsonConvert.DeserializeObject<ApiPositionV2>(postedPositionJson);

            //var pos = await orgClientMock.Object.GetAsync<ApiPositionV2>($"/projects/{Guid.NewGuid()}/positions/{testPosition.Id}");

        }

    }

}
