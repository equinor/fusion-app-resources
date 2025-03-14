using Azure;
using FluentAssertions;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Logic.Commands;
using Fusion.Resources.Logic.Workflows;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using MediatR;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fusion.Services.Org.ApiModels;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Fusion.Resources.Logic.Tests
{

    public class AllocationTests : IDisposable
    {

        ResourcesDbContext dbContext;
        Mock<IOrgApiClient> orgClientMock;
        private readonly Guid testProjectId;
        private readonly Guid draftId;
        private readonly Guid positionId;

        public AllocationTests()
        {
            var options = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase($"{Guid.NewGuid()}")
                .Options;

            dbContext = new ResourcesDbContext(options);
            testProjectId = Guid.NewGuid();
            draftId = Guid.NewGuid();
            positionId = Guid.NewGuid();

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
        public async Task ShouldNotAssignPerson_WhenFutureSplitIsRotation()
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

            patchRequests.Should().HaveCount(1);
            patchRequests.Should().ContainSingle(i => i.Item1.OriginalString.Contains($"{testInstance.Id}"), "Should execute patch request on future instance");
            patchRequests.Should().NotContain(i => i.Item1.OriginalString.Contains($"{rotation_1.Id}"), "Should execute patch request on first rotation split");

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
        public async Task Rotation_ShouldNotAssignPerson_ToSucceedingRotationSplits_WhenSameRotationId()
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

            patchRequests.Should().HaveCount(1);
            patchRequests.Should().NotContain(i => i.Item1.OriginalString.Contains($"{rotation_2_2.Id}"));
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

        [Fact]
        public async Task ShouldNotAssignFutureSplits()
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
                    futureTbnInstance = s.AddInstance(testInstance.AppliesTo.AddDays(1), TimeSpan.FromDays(40));
                });
            });

            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            #endregion

            var patchRequests = await RunProvisioningAsync(request);
            patchRequests.Should().HaveCount(1);
            patchRequests.Should().NotContain(x => x.Item1.OriginalString.Contains(futureTbnInstance.Id.ToString()));
        }

        [Fact]
        public async Task ShouldSendNotificationWhenProvisioning()
        {
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

            var editor = new DbPerson
            {
                AccountType = "Employee",
                AzureUniqueId = testPerson.AzureUniqueId!.Value,
                Name = testPerson.Name
            };
            var request = GenerateRequest(testInstance, r => r.WithProposedPerson(testPerson));
            var workflow = new AllocationNormalWorkflowV1().CreateDatabaseEntity(request.Id, DbRequestType.InternalRequest);
            var wf = WorkflowDefinition.ResolveWorkflow(workflow);
            wf.Step("created").Start();
            wf.SaveChanges();

            var mediatorMock = new Mock<IMediator>(MockBehavior.Loose);
            mediatorMock
                .Setup(x => x.Send(It.IsAny<IRequest<DbWorkflow>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(workflow);

            var handler = new ResourceAllocationRequest.Provision.Handler(
                logger: new Mock<ILogger<ResourceAllocationRequest.Provision.Handler>>().Object,
                dbContext,
                mediator: mediatorMock.Object
            ) as IRequestHandler<ResourceAllocationRequest.Provision>;

            var provisioningReq = new ResourceAllocationRequest.Provision(request.Id);
            provisioningReq.SetEditor(editor.AzureUniqueId, editor);


            await handler.Handle(provisioningReq, CancellationToken.None);

            mediatorMock.Verify(x => x.Publish(It.Is<Events.RequestProvisioned>(x => x.RequestId == request.Id), It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task ShouldDeleteRequestComments()
        {
            #region setup
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
            dbContext.RequestComments.Add(new DbRequestComment
            {
                Comment = "<Insert resource owner gossip here>",
                RequestId = request.Id,
                Origin = DbRequestComment.DbOrigin.Company,
            });
            await dbContext.SaveChangesAsync();
            #endregion

            var handler = new DeleteNotesHandler(dbContext);
            await handler.Handle(new Events.RequestProvisioned(request.Id), CancellationToken.None);

            var rqComments = await dbContext.RequestComments
                .Where(c => c.RequestId == request.Id)
                .ToListAsync();

            rqComments.Should().BeEmpty();
        }

        [Fact]
        public async Task ShouldDeleteRequestActions()
        {
            #region setup
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
            dbContext.RequestActions.Add(new DbRequestAction
            {
                Body = "<Insert resource owner gossip here>",
                RequestId = request.Id,
                Title = "Test title",
                Type = "TestAction"
            });
            await dbContext.SaveChangesAsync();
            #endregion

            var handler = new DeleteActionsHandler(dbContext);
            await handler.Handle(new Events.RequestProvisioned(request.Id), CancellationToken.None);

            var rqComments = await dbContext.RequestActions
                .Where(c => c.RequestId == request.Id)
                .ToListAsync();

            rqComments.Should().BeEmpty();
        }

        [Fact]
        public async Task WithProposedChanges()
        {
            #region setup

            ApiPositionInstanceV2 testInstance = null!;

            var originalWorkload = 50;
            var originalAppliesFrom = DateTime.UtcNow.AddDays(100);
            var originalAppliesTo = originalAppliesFrom.AddDays(200);

            var testPerson = GenerateTestPerson();
            GeneratePosition(p =>
            {
                p.WithInstances(s =>
                {
                    testInstance = s.AddInstance(originalAppliesFrom, originalAppliesTo - originalAppliesFrom).SetExternalId("123");
                    testInstance.Workload = originalWorkload;
                });
            });


            var proposedWorkload = 100;
            var proposedAppliesFrom = originalAppliesFrom.AddDays(10).Date;
            var proposedAppliesTo = originalAppliesTo.Subtract(TimeSpan.FromDays(10));
            var proposedBasePositionId = Guid.NewGuid();
            var proposedLocationId = Guid.NewGuid();

            var request = GenerateRequest(testInstance, r =>
            {
                r.WithProposedPerson(testPerson);

                var proposedChanges = new Dictionary<string, object>()
                {
                    { "workload", proposedWorkload },
                    { "appliesFrom", proposedAppliesFrom.ToString("O") },
                    { "appliesTo", proposedAppliesTo.ToString("O") },
                    { "basePosition", new { id = proposedBasePositionId } },
                    { "location", new { id = proposedLocationId } }
                };

                r.ProposedChanges = JsonConvert.SerializeObject(proposedChanges, Formatting.Indented, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            });

            #endregion

            var patchRequests = await RunProvisioningAsync(request);

            patchRequests.Should().HaveCount(2);
            var positionChange = JsonConvert.DeserializeObject<ApiPositionV2>(patchRequests.First().Item2);
            positionChange.BasePosition.Id.Should().Be(proposedBasePositionId);

            var instanceChange = JsonConvert.DeserializeObject<ApiPositionInstanceV2>(patchRequests.Last().Item2);
            instanceChange.Workload.Should().Be(proposedWorkload);
            instanceChange.AppliesFrom.Should().Be(proposedAppliesFrom);
            instanceChange.AppliesTo.Should().Be(proposedAppliesTo);
            instanceChange.Location.Id.Should().Be(proposedLocationId);
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
            
            // Not including the ?api-version=2.0 as this breaks the regex check.
            orgClientMock.Setup(c => c.SendAsync(MockRequest.GET($"/projects/{testProjectId}/drafts/{draftId}/positions/{testPosition.Id}"))).ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(testPosition), Encoding.UTF8, "application/json")
            });

            return testPosition;
        }

        private ApiPersonProfileV3 GenerateTestPerson() => new FusionTestUserBuilder().SaveProfile();


        private async Task<List<(Uri, string)>> RunProvisioningAsync(DbResourceAllocationRequest request)
        {
            var factoryMock = new Mock<IOrgApiClientFactory>();
            factoryMock.Setup(c => c.CreateClient()).Returns(orgClientMock.Object);

            var cmd = new ResourceAllocationRequest.Allocation.ProvisionAllocationRequest(request.Id);

            // Add telemetry client that does not send any telemetry.
            var mockTelemetryClient = new Microsoft.ApplicationInsights.TelemetryClient(new TelemetryConfiguration() { DisableTelemetry = true });

            var handler = new ResourceAllocationRequest.Allocation.ProvisionAllocationRequest.Handler(mockTelemetryClient, dbContext, factoryMock.Object)
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
