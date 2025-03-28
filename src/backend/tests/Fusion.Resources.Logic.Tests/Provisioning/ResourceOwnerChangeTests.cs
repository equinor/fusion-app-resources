
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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Resources.Domain.Services.OrgClient;
using Fusion.Services.Org.ApiModels;
using Xunit;

namespace Fusion.Resources.Logic.Tests
{
    public class ResourceOwnerProvisioningTests : IDisposable
    {

        ResourcesDbContext dbContext;
        Mock<IOrgApiClient> orgClientMock;

        public ResourceOwnerProvisioningTests() 
        {
            var options = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase($"{Guid.NewGuid()}")
                .Options;

            dbContext = new ResourcesDbContext(options);

            orgClientMock = new Mock<IOrgApiClient>();

            orgClientMock.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Put), CancellationToken.None)).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK
            });
        }

  

        [Fact]
        public async Task ShouldRemoveIdsFromCopiedSplit_WhenChangingEnding()
        {
            #region Setup
            var changeDate = DateTime.Today.AddDays(10);
            var testPerson = GenerateTestPerson();

            var (testInstance, request) = SetupDefaultRemoveTest(changeDate, testPerson);
            #endregion

            var (updatedPosition, json) = await RunProvisioningAsync(request);

            // A new split should have been created
            updatedPosition.Instances.Should().HaveCount(2);
            updatedPosition.Instances.Should().Contain(i => i.Id == Guid.Empty);            // New split
            updatedPosition.Instances.Should().ContainSingle(i => i.ExternalId == "123");   // Should remove the external id for copied split
        }

        [Fact]
        public async Task ShouldRemoveIdsFromCopiedSplit_WhenChangingCenter()
        {
            #region Setup
            ApiPositionInstanceV2 testInstance = null!;
            var changeDate = DateTime.Today.AddDays(10);

            var testPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p => {
                p.WithInstances(s =>
                {
                    testInstance = s.AddInstance(DateTime.UtcNow.AddDays(-100), TimeSpan.FromDays(200))
                        .SetAssignedPerson(testPerson)
                        .SetExternalId("123");
                });
            });

            var request = GenerateRequest(testInstance, r => r
                .AsResourceRemoval()
                .WithChangeRange(changeDate, changeDate.AddDays(5)));
            #endregion

            var (updatedPosition, json) = await RunProvisioningAsync(request);

            // A new split should have been created
            updatedPosition.Instances.Should().HaveCount(3);                                // Three splits should be created
            updatedPosition.Instances.Should().ContainSingle(i => i.ExternalId == "123");   // Should remove the external id for copied split
            updatedPosition.Instances.Count(i => i.Id == Guid.Empty).Should().Be(2);        // New split
        }

        [Fact]
        public async Task ShouldCreateNewInstance_WhenChangingActiveInstance()
        {
            #region Setup
            var changeDate = DateTime.Today.AddDays(10);
            var testPerson = GenerateTestPerson();

            var (testInstance, request) = SetupDefaultRemoveTest(changeDate, testPerson);
            #endregion

            var (updatedPosition, json) = await RunProvisioningAsync(request);

            // A new split should have been created
            updatedPosition.Instances.Should().HaveCount(2);

            updatedPosition.Instances.Should().Contain(i => i.AppliesFrom.Date == changeDate && i.AppliesTo.Date == testInstance.AppliesTo.Date);
            updatedPosition.Instances.Should().Contain(i => i.AppliesFrom.Date == testInstance.AppliesFrom.Date &&  i.AppliesTo.Date == changeDate.AddDays(-1));
            updatedPosition.Instances.Should().ContainSingle(i => i.AssignedPerson == null);    // Should have removed person
            updatedPosition.Instances.Should().ContainSingle(i => i.AssignedPerson != null && i.AssignedPerson.AzureUniqueId == testPerson.AzureUniqueId);
        }

        [Fact]
        public async Task ShouldSetCorrectInstanceDates_WhenUpdatingCenter()
        {
            #region Setup
            var changeDate = DateTime.Today.AddDays(10);
            var changeDateEnd = changeDate.AddDays(5);

            var (testInstance, testPerson) = SetupDefaultTestPosition();

            var request = GenerateRequest(testInstance, r => r
                .AsResourceRemoval()
                .WithChangeRange(changeDate, changeDateEnd));
            #endregion

            var (updatedPosition, json) = await RunProvisioningAsync(request);

            // A new split should have been created
            updatedPosition.Instances.Should().HaveCount(3);

            updatedPosition.Instances.Should().Contain(i => i.AppliesFrom == changeDate && i.AppliesTo == changeDateEnd);                               // The changed split
            updatedPosition.Instances.Should().Contain(i => i.AppliesFrom == testInstance.AppliesFrom && i.AppliesTo == changeDate.AddDays(-1));        // The current split
            updatedPosition.Instances.Should().Contain(i => i.AppliesFrom == changeDateEnd.AddDays(1) && i.AppliesTo == testInstance.AppliesTo);        // The remained split
        }

        [Fact]
        public async Task ChangeResource_ShouldUpdateAssignedPerson()
        {
            ApiPositionInstanceV2 testInstance = null!;
            var changeDate = DateTime.Today.AddDays(10);

            var testPerson = GenerateTestPerson();
            var newTestPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p => {
                p.WithInstances(s =>
                {
                    testInstance = s.AddInstance(DateTime.Today.AddDays(-100), TimeSpan.FromDays(200))
                        .SetAssignedPerson(testPerson)
                        .SetExternalId("123");
                });
            });

            var request = GenerateRequest(testInstance, r => r
                .AsResourceChange(newTestPerson)
                .WithChangeDate(changeDate));

            var (updatedPosition, json) = await RunProvisioningAsync(request);

            updatedPosition.Instances.Should().ContainSingle(i => i.AssignedPerson.AzureUniqueId == newTestPerson.AzureUniqueId);    // Should have removed person
            updatedPosition.Instances.Should().ContainSingle(i => i.AssignedPerson.AzureUniqueId == testPerson.AzureUniqueId);
        }

        [Fact]
        public async Task Adjustment_ShouldUpdateCorrectly_WhenTemporaryChange()
        {
            #region Setup
            var changeDate = DateTime.Today.AddDays(10);
            var changeDateEnd = changeDate.AddDays(5);

            var (testInstance, testPerson) = SetupDefaultTestPosition();

            var request = GenerateRequest(testInstance, r => r
                .AsAdjustment(new { 
                    workload = 50 
                })
                .WithChangeRange(changeDate, changeDateEnd));
            #endregion

            var (updatedPosition, json) = await RunProvisioningAsync(request);

            // A new split should have been created
            updatedPosition.Instances.Should().HaveCount(3);

            var changedInstance = updatedPosition.Instances.First(i => i.AppliesFrom == changeDate && i.AppliesTo == changeDateEnd);
            var existingSplit = updatedPosition.Instances.First(i => i.AppliesFrom == testInstance.AppliesFrom && i.AppliesTo == changeDate.AddDays(-1));
            var remainderSplit = updatedPosition.Instances.First(i => i.AppliesFrom == changeDateEnd.AddDays(1) && i.AppliesTo == testInstance.AppliesTo);

            changedInstance.Workload.Should().Be(50);
            existingSplit.Workload.Should().Be(100);
            remainderSplit.Workload.Should().Be(100);
        }

        #region Setups
        private (ApiPositionInstanceV2, DbResourceAllocationRequest) SetupDefaultRemoveTest(DateTime changeDate, ApiPersonProfileV3 testPerson)
        {
            ApiPositionInstanceV2 testInstance = null!;

            var testPosition = GeneratePosition(p => {
                p.WithInstances(s =>
                {
                    testInstance = s.AddInstance(DateTime.Today.AddDays(-100), TimeSpan.FromDays(200))
                        .SetAssignedPerson(testPerson)
                        .SetExternalId("123");
                });
            });

            var request = GenerateRequest(testInstance, r => r
                .AsResourceRemoval()
                .WithChangeDate(changeDate));

            return (testInstance, request);
        }
        private (ApiPositionInstanceV2, ApiPersonProfileV3) SetupDefaultTestPosition()
        {
            ApiPositionInstanceV2 testInstance = null!;

            var testPerson = GenerateTestPerson();
            var testPosition = GeneratePosition(p => {
                p.WithInstances(s =>
                {
                    testInstance = s.AddInstance(DateTime.UtcNow.AddDays(-100), TimeSpan.FromDays(200))
                        .SetWorkload(100)
                        .SetAssignedPerson(testPerson)
                        .SetExternalId("123");
                });
            });

            return (testInstance, testPerson);
        }

        #endregion

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
                Type = Database.Entities.DbInternalRequestType.ResourceOwnerChange,
                SubType = null,
                ProposalParameters = new Database.Entities.DbResourceAllocationRequest.DbOpProposalParameters()
                {
                    ChangeFrom = DateTime.UtcNow.AddDays(10)
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
                    OrgProjectId = Guid.Empty,
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

            orgClientMock.Setup(c => c.SendAsync(It.Is<HttpRequestMessage>(m => m.Method == HttpMethod.Get), CancellationToken.None)).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(testPosition), Encoding.UTF8, "application/json")
            });

            return testPosition;
        }

        private ApiPersonProfileV3 GenerateTestPerson() => new FusionTestUserBuilder().SaveProfile();


        private async Task<(ApiPositionV2, string)> RunProvisioningAsync(DbResourceAllocationRequest request)
        {
            var factoryMock = new Mock<IOrgApiClientFactory>();
            factoryMock.Setup(c => c.CreateClient()).Returns(orgClientMock.Object);

            var cmd = new ResourceAllocationRequest.ResourceOwner.ProvisionResourceOwnerRequest(request.Id);
            var handler = new ResourceAllocationRequest.ResourceOwner.ProvisionResourceOwnerRequest.Handler(dbContext, factoryMock.Object)
                as IRequestHandler<ResourceAllocationRequest.ResourceOwner.ProvisionResourceOwnerRequest>;
            await handler.Handle(cmd, CancellationToken.None);

            var i = orgClientMock.Invocations.SelectMany(i => i.Arguments.Cast<HttpRequestMessage>())
                .FirstOrDefault(m => m.Method == HttpMethod.Put);
            var postedPositionJson = await i.Content.ReadAsStringAsync();
            var postedPosition = JsonConvert.DeserializeObject<ApiPositionV2>(postedPositionJson);

            return (postedPosition, postedPositionJson);
        }
        #endregion
    }

}
