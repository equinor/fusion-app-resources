using FluentAssertions;
using Fusion.ApiClients.Org;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
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

namespace Fusion.Resources.Logic.Tests
{

    public class AllocationTests : IDisposable
    {

        ResourcesDbContext dbContext;
        Mock<IOrgApiClient> orgClientMock;
        private readonly Guid testProjectId;
        private readonly Guid draftId;

        public AllocationTests() 
        {
            var options = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase($"{Guid.NewGuid()}")
                .Options;

            dbContext = new ResourcesDbContext(options);
            testProjectId = Guid.NewGuid();
            draftId = Guid.NewGuid();

            orgClientMock = new Mock<IOrgApiClient>();

            orgClientMock.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Patch))).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK
            });

            // Must mock the draft & publish
            orgClientMock.Setup(c => c.SendAsync(MockRequest.POST($"/projects/{testProjectId}/drafts"))).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new ApiDraftV2() { Id = draftId, ProjectId = testProjectId }), Encoding.UTF8, "application/json")
            });

            orgClientMock.Setup(c => c.SendAsync(MockRequest.POST($"/projects/{testProjectId}/drafts/{draftId}/publish"))).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new ApiDraftV2() { Id = draftId, Status = "Published" }), Encoding.UTF8, "application/json")
            });
        }


        [Fact]
        public async Task ShouldAssignPerson_WhenFutureInstance()
        {
            #region Setup
            ApiPositionInstanceV2 testInstance = null!;

            var testPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p =>
            {
                p.WithInstances(s =>
                {
                    // Future instance
                    testInstance = s.AddInstance(DateTime.UtcNow.AddDays(100), TimeSpan.FromDays(200))
                        .SetExternalId("123");
                });
            });

            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            #endregion

            var patchRequests = await RunProvisioningAsync(request);

            patchRequests.Should().HaveCount(1);
            var patchPayload = JsonConvert.DeserializeAnonymousType(patchRequests.First().Item2, new { assignedPerson = new { azureUniqueId = Guid.Empty } });
            patchPayload.assignedPerson.azureUniqueId.Should().Be(testPerson.AzureUniqueId.Value);
        }

        [Fact]
        public async Task ShouldAssignPerson_WhenFutureSplitIsRotation()
        {
            #region Setup
            ApiPositionInstanceV2 testInstance = null!;
            ApiPositionInstanceV2 rotation_1 = null!;
            ApiPositionInstanceV2 rotation_2 = null!;
            ApiPositionInstanceV2 rotation_3 = null!;

            var testPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p =>
            {
                p.WithInstances(s =>
                {
                    // Future instance
                    testInstance = s.AddInstance(DateTime.UtcNow.AddDays(10), TimeSpan.FromDays(20));
                    rotation_1 = s.AddInstance(testInstance.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(1);
                    rotation_2 = s.AddInstance(testInstance.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(2);
                    rotation_3 = s.AddInstance(testInstance.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(3);
                });
            });

            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            #endregion

            var patchRequests = await RunProvisioningAsync(request);

            patchRequests.Should().HaveCount(2);
            patchRequests.Should().ContainSingle(i => i.Item1.OriginalString.Contains($"{testInstance.Id}"), "Should execute patch request on future instance");
            patchRequests.Should().ContainSingle(i => i.Item1.OriginalString.Contains($"{rotation_1.Id}"), "Should execute patch request on first rotation split");

        }

        [Fact]
        public async Task ShouldAssignPerson_WhenRotationSplit_RotationId_2()
        {
            #region Setup
            ApiPositionInstanceV2 onShore = null!;
            ApiPositionInstanceV2 testInstance = null!;
            ApiPositionInstanceV2 rotation_1 = null!;
            ApiPositionInstanceV2 rotation_3 = null!;

            var testPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p =>
            {
                p.WithInstances(s =>
                {
                    // Future instance
                    onShore = s.AddInstance(DateTime.UtcNow.AddDays(10), TimeSpan.FromDays(20));
                    rotation_1 = s.AddInstance(onShore.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(1);
                    testInstance = s.AddInstance(onShore.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(2);
                    rotation_3 = s.AddInstance(onShore.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(3);
                });
            });

            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            #endregion

            var patchRequests = await RunProvisioningAsync(request);

            patchRequests.Should().HaveCount(1);
            patchRequests.Should().ContainSingle(i => i.Item1.OriginalString.Contains($"{testInstance.Id}"), "Should only assign to targeted rotation split");
        }

        [Fact]
        public async Task Rotation_ShouldAssignPerson_ToSucceedingRotationSplits_WhenSameRotationId()
        {
            #region Setup
            ApiPositionInstanceV2 onShore = null!;
            ApiPositionInstanceV2 testInstance = null!;
            ApiPositionInstanceV2 rotation_1 = null!;
            ApiPositionInstanceV2 rotation_2_2 = null!;
            ApiPositionInstanceV2 rotation_3 = null!;

            var testPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p =>
            {
                p.WithInstances(s =>
                {
                    // Future instance
                    onShore = s.AddInstance(DateTime.UtcNow.AddDays(10), TimeSpan.FromDays(20));
                    rotation_1 = s.AddInstance(onShore.AppliesTo.AddDays(1), TimeSpan.FromDays(40)).SetRotation(1);
                    testInstance = s.AddInstance(onShore.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(2);
                    rotation_2_2 = s.AddInstance(testInstance.AppliesTo.AddDays(1), TimeSpan.FromDays(20)).SetRotation(2);
                    rotation_3 = s.AddInstance(onShore.AppliesTo.AddDays(1), TimeSpan.FromDays(40)).SetRotation(3);
                });
            });

            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            #endregion

            var patchRequests = await RunProvisioningAsync(request);

            patchRequests.Should().HaveCount(2);
            patchRequests.Should().ContainSingle(i => i.Item1.OriginalString.Contains($"{testInstance.Id}"));
            patchRequests.Should().ContainSingle(i => i.Item1.OriginalString.Contains($"{rotation_2_2.Id}"));
        }

        [Fact]
        public async Task ShouldAssignPersonToSucceedingInstance_WhenTbn()
        {
            #region Setup
            ApiPositionInstanceV2 testInstance = null!;
            ApiPositionInstanceV2 futureTbnInstance = null!;


            var testPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p =>
            {
                p.WithInstances(s =>
                {
                    // Future instance
                    testInstance = s.AddInstance(DateTime.UtcNow.AddDays(10), TimeSpan.FromDays(20));
                    futureTbnInstance = s.AddInstance(testInstance.AppliesTo.AddDays(1), TimeSpan.FromDays(40));
                });
            });

            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            #endregion

            var patchRequests = await RunProvisioningAsync(request);

            patchRequests.Should().HaveCount(2);
            patchRequests.Should().ContainSingle(i => i.Item1.OriginalString.Contains($"{futureTbnInstance.Id}"), "Should execute patch request on future instance");
        }

        [Fact]
        public async Task ShouldNotAssignPersonToSucceedingInstance_WhenAssigned()
        {
            #region Setup
            ApiPositionInstanceV2 testInstance = null!;
            ApiPositionInstanceV2 futureTbnInstance = null!;


            var testPerson = GenerateTestPerson();
            var otherTestPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p =>
            {
                p.WithInstances(s =>
                {
                    // Future instance
                    testInstance = s.AddInstance(DateTime.UtcNow.AddDays(10), TimeSpan.FromDays(20));
                    futureTbnInstance = s.AddInstance(testInstance.AppliesTo.AddDays(1), TimeSpan.FromDays(40)).SetAssignedPerson(otherTestPerson);
                });
            });

            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            #endregion

            var patchRequests = await RunProvisioningAsync(request);

            patchRequests.Should().HaveCount(1);
            patchRequests.Should().NotContain(i => i.Item1.OriginalString.Contains($"{futureTbnInstance.Id}"), "Should execute patch request on future instance");
        }


        #region Helpers

        public void Dispose()
        {
            dbContext.Dispose();
        }


        private DbResourceAllocationRequest GenerateRequest(ApiPositionInstanceV2 instance, Action<DbResourceAllocationRequest> setup = null)
        {
            var request = new Database.Entities.DbResourceAllocationRequest()
            {
                Id = Guid.NewGuid(),
                Type = Database.Entities.DbInternalRequestType.Allocation,
                SubType = "normal",
                ProposalParameters = new Database.Entities.DbResourceAllocationRequest.DbOpProposalParameters()
                {
                },
                OrgPositionId = instance.PositionId,
                OrgPositionInstance = new Database.Entities.DbResourceAllocationRequest.DbOpPositionInstance
                {
                    AppliesFrom = instance.AppliesFrom,
                    AppliesTo = instance.AppliesTo,
                    AssignedToMail = instance.AssignedPerson?.Mail,
                    AssignedToUniqueId = instance.AssignedPerson?.AzureUniqueId,
                    Id = instance.Id,
                    Obs = instance.Obs,
                    Workload = instance.Workload
                },
                Project = new Database.Entities.DbProject
                {
                    OrgProjectId = testProjectId,
                    Name = "Test project"
                }
            };

            setup?.Invoke(request);

            dbContext.ResourceAllocationRequests.Add(request);
            dbContext.SaveChanges();

            return request;
        }

        private ApiPositionV2 GeneratePosition(Action<ApiPositionV2> setup)
        {
            var testPosition = PositionBuilder.NewPosition();

            setup(testPosition);

            orgClientMock.Setup(c => c.GetPositionV2Async(It.Is<OrgProjectId>(id => id.ProjectId == testProjectId), testPosition.Id, null)).ReturnsAsync(testPosition);

            return testPosition;
        }

        private ApiPersonProfileV3 GenerateTestPerson() => new FusionTestUserBuilder().SaveProfile();


        private async Task<IEnumerable<(Uri, string)>> RunProvisioningAsync(DbResourceAllocationRequest request)
        {
            var factoryMock = new Mock<IOrgApiClientFactory>();
            factoryMock.Setup(c => c.CreateClient(ApiClientMode.Application)).Returns(orgClientMock.Object);

            var cmd = new ResourceAllocationRequest.Allocation.ProvisionAllocationRequest(request.Id);
            var handler = new ResourceAllocationRequest.Allocation.ProvisionAllocationRequest.Handler(dbContext, factoryMock.Object)
                as IRequestHandler<ResourceAllocationRequest.Allocation.ProvisionAllocationRequest>;
            await handler.Handle(cmd, CancellationToken.None);


            var i = orgClientMock.Invocations.SelectMany(i => i.Arguments.OfType<HttpRequestMessage>())
                .Where(m => m.Method == HttpMethod.Patch);

            var patchRequests = new List<(Uri, string)>();
            foreach (var req in i)
            {
                patchRequests.Add((req.RequestUri, await req.Content.ReadAsStringAsync()));
            }
            return patchRequests;
        }
        #endregion
    }

}
