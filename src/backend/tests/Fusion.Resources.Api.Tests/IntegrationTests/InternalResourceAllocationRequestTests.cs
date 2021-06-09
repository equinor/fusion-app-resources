using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Resources.Api.Tests.Fixture;
using Fusion.Testing;
using Fusion.Testing.Authentication.User;
using Fusion.Testing.Mocks;
using Fusion.Testing.Mocks.OrgService;
using Fusion.Testing.Mocks.ProfileService;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
#nullable enable 

namespace Fusion.Resources.Api.Tests.IntegrationTests
{
    public class InternalResourceAllocationRequestTests : IClassFixture<ResourceApiFixture>, IAsyncLifetime
    {
        const string TestDepartmentId = "TPD PRD FE MMS MAT1";

        private readonly ResourceApiFixture fixture;
        private readonly TestLoggingScope loggingScope;

        /// <summary>
        ///     Will be generated new for each test
        /// </summary>
        private ApiPersonProfileV3 testUser;


        // Created by the async lifetime
        private TestApiInternalRequestModel normalRequest = null!;
        private FusionTestProjectBuilder testProject = null!;

        private Guid projectId => testProject.Project.ProjectId;

        public InternalResourceAllocationRequestTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;

            // Make the output channel available for TestLogger.TryLog and the TestClient* calls.
            loggingScope = new TestLoggingScope(output);

            // Generate random test user
            testUser = fixture.AddProfile(FusionAccountType.External);

            fixture.EnsureDepartment(TestDepartmentId);
        }

        private HttpClient Client => fixture.ApiFactory.CreateClient();

        public async Task InitializeAsync()
        {
            // Mock profile
            testUser = PeopleServiceMock.AddTestProfile()
                .SaveProfile();

            // Mock project
            testProject = new FusionTestProjectBuilder()
                .WithPositions(200)
                .AddToMockService();

            // Prepare context resolver.
            fixture.ContextResolver
                .AddContext(testProject.Project);

            // Prepare admin client
            var adminClient = fixture.ApiFactory.CreateClient()
                .WithTestUser(fixture.AdminUser)
                .AddTestAuthToken();

            // Create a default request we can work with
            normalRequest = await adminClient.CreateDefaultRequestAsync(testProject);


            //fixture.GetNotificationMessages< Integration.Models.FusionEvents.ResourceAllocationRequestSubscriptionEvent >("resources-sub")
            //    .Should().Contain(m => m.Payload.ItemId == normalRequest.Id && m.Payload.Type == Integration.Models.FusionEvents.EventType.RequestCreated);
            //var commentResponse = await adminClient.TestClientPostAsync($"/resources/requests/internal/{normalRequest.Request.Id}/comments", new { Content = "Normal test request comment" }, new { Id = Guid.Empty });
            //commentResponse.Should().BeSuccessfull();
            //testCommentId = commentResponse.Value.Id;
        }

        public Task DisposeAsync()
        {
            loggingScope.Dispose();

            return Task.CompletedTask;
        }

        #region delete tests
        [Fact]
        public async Task Delete_InternalRequest_ShouldBeSuccessfull()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientDeleteAsync($"/resources/requests/internal/{normalRequest.Id}");
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task Delete_InternalRequest_ShouldSendSubscriptionEvent()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientDeleteAsync($"/resources/requests/internal/{normalRequest.Id}");

            fixture.GetNotificationMessages<Integration.Models.FusionEvents.ResourceAllocationRequestSubscriptionEvent>("resources-sub")
                .Should().Contain(m => m.Payload.ItemId == normalRequest.Id && m.Payload.Type == Integration.Models.FusionEvents.EventType.RequestRemoved);
        }

        [Fact]
        public async Task Delete_InternalRequest_NonExistingRequest_ShouldBeNotFound()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientDeleteAsync($"/resources/requests/internal/{Guid.NewGuid()}");
            response.Should().BeNotFound();
        }
        [Fact]
        public async Task Delete_RequestComment_ShouldBeDeleted()
        {
            using var adminScope = fixture.AdminScope();

            var commentResponse = await Client.TestClientPostAsync($"/resources/requests/internal/{normalRequest.Id}/comments", new
            {
                Content = "Normal test request comment"
            }, new { Id = Guid.Empty });
            commentResponse.Should().BeSuccessfull();
            var commentId = commentResponse.Value.Id;

            var response = await Client.TestClientDeleteAsync($"/resources/requests/internal/{normalRequest.Id}/comments/{commentId}");
            response.Should().BeSuccessfull();
        }
        [Fact]
        public async Task Delete_NonExistingRequestComment_ShouldBeNotFound()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientDeleteAsync($"/resources/requests/internal/{normalRequest.Id}/comments/{Guid.NewGuid()}");
            response.Should().BeNotFound();
        }
        #endregion

        #region get tests
        [Fact]
        public async Task Get_ProjectRequest_ShouldBeSuccessfull_WhenAdmin()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<TestApiInternalRequestModel>($"/projects/{projectId}/requests/{normalRequest.Id}");
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task CreateRequest_ShouldSet_AdditionalNote()
        {
            using var adminScope = fixture.AdminScope();

            var testNote = "Test note";

            var request = await Client.CreateDefaultRequestAsync(testProject, r => r.WithAdditionalNote(testNote));
            request.AdditionalNote.Should().Be(testNote);
        }

        [Fact]
        public async Task Get_ProjectRequests_ShouldReturnDraftRequests()
        {
            using var adminScope = fixture.AdminScope();

            var request = await Client.CreateDefaultRequestAsync(testProject);

            var topResponseTest = await Client.TestClientGetAsync($"/projects/{projectId}/requests", new
            {
                value = new[] { new { id = Guid.Empty } }
            });
            topResponseTest.Should().BeSuccessfull();
            topResponseTest.Value.value.Should().Contain(r => r.id == request.Id);
        }

        [Fact]
        public async Task GetRequest_ShouldExpandTaskOwner_WhenTaskOwnerExists()
        {
            using var adminScope = fixture.AdminScope();

            var taskOwnerPerson = fixture.AddProfile(FusionAccountType.Employee);
            var taskOwnerPosition = testProject.AddPosition().WithEnsuredFutureInstances().WithAssignedPerson(taskOwnerPerson);
            var requestPosition = testProject.AddPosition().WithEnsuredFutureInstances();
            testProject.SetTaskOwner(requestPosition.Id, taskOwnerPosition.Id);
            var request = await Client.CreateRequestAsync(projectId, r => r.AsTypeNormal().WithPosition(requestPosition));

            var result = await Client.TestClientGetAsync($"/resources/requests/internal/{request.Id}?$expand=taskowner", new
            {
                taskOwner = new
                {
                    date = DateTime.Now,
                    positionId = (Guid?)null,
                    instanceIds = Array.Empty<Guid>(),
                    persons = new[] { new { mail = string.Empty } }
                }
            });


            result.Should().BeSuccessfull();
            result.Value.taskOwner.Should().NotBeNull();
            result.Value.taskOwner.positionId.Should().Be(taskOwnerPosition.Id);
            result.Value.taskOwner.persons.Should().Contain(p => p.mail == taskOwnerPerson.Mail);
        }

        [Fact]
        public async Task GetRequest_ShouldIncludeDepartment_WhenExpanded()
        {
            using var adminScope = fixture.AdminScope();
            const string expectedDepartment = "TPD PRD TST QWE";

            fixture.EnsureDepartment(expectedDepartment);
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            fixture.ApiFactory.lineOrgMock.WithResponse("/lineorg/persons", new
            {
                Count = 1,
                TotalCount = 1,
                Value = new[]
                {
                    new
                    {
                        fakeResourceOwner.AzureUniqueId,
                        fakeResourceOwner.Name,
                        fakeResourceOwner.Mail,
                        IsResourceOwner = true,
                        FullDepartment = expectedDepartment
                    }
                }
            });

            var requestPosition = testProject.AddPosition().WithEnsuredFutureInstances();
            var request = await Client.CreateRequestAsync(projectId, r => r.AsTypeNormal().WithPosition(requestPosition));
            await Client.StartProjectRequestAsync(testProject, request.Id);
            request = await Client.AssignDepartmentAsync(request.Id, expectedDepartment);

            var result = await Client.TestClientGetAsync($"/resources/requests/internal/{request.Id}?$expand=DepartmentDetails", new
            {
                assignedDepartmentDetails = new
                {
                    name = "",
                    lineOrgResponsible = new { azureUniqueId = Guid.Empty, name = "" }
                }
            });


            result.Should().BeSuccessfull();
            result.Value.assignedDepartmentDetails.Should().NotBeNull();

            result.Value.assignedDepartmentDetails.name.Should().Be(expectedDepartment);
            result.Value.assignedDepartmentDetails.lineOrgResponsible.name.Should().Be(fakeResourceOwner.Name);
        }

        [Fact]
        public async Task GetRequest_ShouldUseInstanceStartDate_WhenExpandTaskOwner()
        {
            using var adminScope = fixture.AdminScope();

            // Create a task owner position that has different persons assigned currently and when the requested instance starts.
            // We should expect that the future task owner is returned.
            var currentTaskOwnerPerson = fixture.AddProfile(FusionAccountType.Employee);
            var futureTaskOwnerPerson = fixture.AddProfile(FusionAccountType.Employee);
            var taskOwnerPosition = testProject.AddPosition().WithInstances(ib =>
            {
                ib.AddInstance(DateTime.UtcNow.AddDays(-10).Date, TimeSpan.FromDays(20)).SetAssignedPerson(currentTaskOwnerPerson);
                ib.AddInstance(DateTime.UtcNow.AddDays(21).Date, TimeSpan.FromDays(20)).SetAssignedPerson(futureTaskOwnerPerson);
            });

            // Create the position so that the instance starts when the future task owner is assigned.
            var requestPosition = testProject.AddPosition().WithInstances(ib =>
            {
                ib.AddInstance(DateTime.UtcNow.AddDays(40), TimeSpan.FromDays(10));
            });

            testProject.SetTaskOwner(requestPosition.Id, taskOwnerPosition.Id);
            var request = await Client.CreateRequestAsync(projectId, r => r.AsTypeNormal().WithPosition(requestPosition));

            var result = await Client.TestClientGetAsync($"/resources/requests/internal/{request.Id}?$expand=taskowner", new
            {
                taskOwner = new
                {
                    date = DateTime.Now,
                    positionId = (Guid?)null,
                    instanceIds = Array.Empty<Guid>(),
                    persons = new[] { new { mail = string.Empty } }
                }
            });


            result.Should().BeSuccessfull();
            result.Value.taskOwner.Should().NotBeNull();
            result.Value.taskOwner.positionId.Should().Be(taskOwnerPosition.Id);
            result.Value.taskOwner.persons.Should().Contain(p => p.mail == futureTaskOwnerPerson.Mail);
        }


        [Fact]
        public async Task Get_ProjectRequests_ShouldNotExpandPositionByDefault()
        {
            using var adminScope = fixture.AdminScope();
            var plainList = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>($"/projects/{projectId}/requests");
            plainList.Should().BeSuccessfull();
            foreach (var m in plainList.Value.Value)
            {
                m.OrgPosition.Should().BeNull();
                m.OrgPositionInstance.Should().BeNull();
            }
        }

        [Fact]
        public async Task Get_ProjectRequests_ShouldIncludePosition_WhenExpanded()
        {
            using var adminScope = fixture.AdminScope();
            var plainList = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>($"/projects/{projectId}/requests?$expand=orgPosition");
            plainList.Should().BeSuccessfull();
            foreach (var m in plainList.Value.Value)
            {
                m.OrgPosition.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Get_ProjectRequests_ShouldIncludePositionInstance_WhenExpanded()
        {
            using var adminScope = fixture.AdminScope();
            var plainList = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>($"/projects/{projectId}/requests?$expand=orgPositionInstance");
            plainList.Should().BeSuccessfull();
            foreach (var m in plainList.Value.Value)
            {
                m.OrgPositionInstance.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task Get_ProjectRequests_ShouldIncludePositionAndInstance_WhenBothExpanded()
        {
            using var adminScope = fixture.AdminScope();

            var expandedAllList = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>($"/projects/{projectId}/requests?$expand=orgPositionInstance,orgPosition");
            expandedAllList.Should().BeSuccessfull();
            foreach (var m in expandedAllList.Value.Value)
            {
                m.OrgPosition.Should().NotBeNull();
                m.OrgPositionInstance.Should().NotBeNull();
            }
        }

        [Fact]
        public async Task GetAllRequests_ShouldReturnEverything()
        {
            using var adminScope = fixture.AdminScope();
            var response = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>($"/resources/requests/internal");
            response.Should().BeSuccessfull();

            response.Value.Value.Count().Should().BeGreaterThan(0);

        }

        //[Fact]
        //public async Task UnassignedRequests_ShouldReturnRequestsWithoutDepartment()
        //{
        //    using var adminScope = fixture.AdminScope();

        //    var newRequestId = await Client.CreateDefaultRequestAsync(testProject, r => r.WithAssignedDepartment(null));

        //    var response = await Client.TestClientGetAsync($"/resources/requests/internal/unassigned", new { value = new[] { new { id = Guid.Empty, assignedDepartment = string.Empty } } });
        //    response.Should().BeSuccessfull();

        //    response.Value.value.Should().Contain(r => r.id == newRequestId);
        //    response.Value.value.Should().OnlyContain(r => r.assignedDepartment == null);
        //}

        //[Fact]
        //public async Task UnassignedRequests_ShouldReturnCountOnly_WhenCountQueryParameter()
        //{
        //    using var adminScope = fixture.AdminScope();

        //    for (int i = 0; i < 10; i++) await Client.CreateRequestAsync(testProject, r => r.WithAssignedDepartment(null));


        //    var response = await Client.TestClientGetAsync($"/resources/requests/internal/unassigned?$count=only", new { value = Array.Empty<object>(), totalCount = 0 });
        //    response.Should().BeSuccessfull();

        //    response.Value.totalCount.Should().BeGreaterOrEqualTo(10);
        //    response.Value.value.Should().BeEmpty();
        //}

        //[Fact]
        //public async Task UnassignedRequests_ShouldNotReturnDraftRequests_WhenUsingGlobal()
        //{
        //    using var adminScope = fixture.AdminScope();

        //    var newRequestId = await Client.CreateRequestAsync(testProject, r => r.WithAssignedDepartment(null).WithIsDraft(true));

        //    var response = await Client.TestClientGetAsync($"/resources/requests/internal/unassigned", new { value = new[] { new { id = Guid.Empty, assignedDepartment = string.Empty } } });
        //    response.Should().BeSuccessfull();

        //    response.Value.value.Should().NotContain(r => r.id == newRequestId);
        //}


        #endregion

        #region put tests
        //[Fact]
        //public async Task Put_ProjectRequest_ShouldBeAuthorized()
        //{
        //    using var adminScope = fixture.AdminScope();
        //    var beforeUpdate = DateTimeOffset.UtcNow;
        //    var dict = new Dictionary<string, object> { { "orgpositioninstance.workload", 50 } };
        //    var updateRequest = new UpdateResourceAllocationRequest
        //    {
        //        ProjectId = normalRequest.Project.ProjectId,
        //        OrgPositionId = normalRequest.Request.OrgPositionId,
        //        OrgPositionInstance = normalRequest.Request.OrgPositionInstance,
        //        AssignedDepartment = "TPD",
        //        Discipline = "upd",
        //        IsDraft = false,
        //        AdditionalNote = "upd",
        //        ProposedPersonAzureUniqueId = normalRequest.Request.ProposedPersonAzureUniqueId,
        //        ProposedChanges = new ApiPropertiesCollection(dict)
        //    };

        //    var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}", updateRequest);
        //    response.Should().BeSuccessfull();

        //    AssertPropsAreEqual(response.Value, updateRequest, adminScope);
        //    response.Value.Updated?.Should().BeAfter(beforeUpdate);
        //}
        //[Fact]
        //public async Task Put_ProjectRequest_InvalidRequest_ShouldBeUnsuccessful()
        //{
        //    using var adminScope = fixture.AdminScope();

        //    var updateRequest = new UpdateResourceAllocationRequest { ProposedPersonAzureUniqueId = Guid.Empty };
        //    var response = await Client.TestClientPutAsync<PagedCollection<ResourceAllocationRequestTestModel>>($"/projects/{normalRequest.Project.ProjectId}/requests/{normalRequest.Request.Id}", updateRequest);
        //    response.Should().BeBadRequest("Invalid arguments passed");

        //}
        //[Fact]
        //public async Task Put_InternalRequest_ShouldBeAuthorized()
        //{
        //    using var adminScope = fixture.AdminScope();
        //    var beforeUpdate = DateTimeOffset.UtcNow;

        //    var updateRequest = new UpdateResourceAllocationRequest
        //    {
        //        ProjectId = normalRequest.Project.ProjectId,
        //        OrgPositionId = normalRequest.Request.OrgPositionId,
        //        OrgPositionInstance = normalRequest.Request.OrgPositionInstance,
        //        Discipline = "upd",
        //        IsDraft = false,
        //        AdditionalNote = "upd",
        //        ProposedPersonAzureUniqueId = normalRequest.Request.ProposedPersonAzureUniqueId
        //    };

        //    var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/resources/requests/internal/{normalRequest.Request.Id}", updateRequest);
        //    response.Should().BeSuccessfull();

        //    AssertPropsAreEqual(response.Value, updateRequest, adminScope);
        //    response.Value.Updated?.Should().BeAfter(beforeUpdate);
        //}
        //[Fact]
        //public async Task Put_InternalRequest_EmptyRequest_ShouldNotModifyDbEntity()
        //{
        //    using var adminScope = fixture.AdminScope();
        //    var updateRequest = new UpdateResourceAllocationRequest();
        //    var response = await Client.TestClientPutAsync<ResourceAllocationRequestTestModel>($"/resources/requests/internal/{normalRequest.Request.Id}", updateRequest);
        //    response.Value.Updated.Should().BeNull();
        //}

        //[Fact]
        //public async Task Put_RequestComment_ShouldBeUpdated()
        //{
        //    using var adminScope = fixture.AdminScope();

        //    var response = await Client.TestClientPutAsync<ObjectWithId>($"/resources/requests/internal/{normalRequest.Request.Id}/comments/{testCommentId}", new { Content = "Updated normal comment" });
        //    response.Should().BeSuccessfull();
        //}
        #endregion

        #region Update request

        [Theory]
        [InlineData("additionalNote", "Some test note")]
        [InlineData("assignedDepartment", TestDepartmentId)]
        [InlineData("proposedPersonAzureUniqueId", null)]
        public async Task UpdateRequest_ShouldUpdate_WhenPatching(string property, object value)
        {
            using var adminScope = fixture.AdminScope();

            if (property == "proposedPersonAzureUniqueId")
                value = this.testUser.AzureUniqueId!.Value;

            var request = await Client.CreateDefaultRequestAsync(testProject);

            JObject payload = new JObject();
            payload.Add(property, JToken.FromObject(value));


            var response = await Client.TestClientPatchAsync<JObject>($"/resources/requests/internal/{request.Id}", payload);
            response.Should().BeSuccessfull();

            var updatedProp = response.Value.Property(property)?.ToObject(value.GetType());
            updatedProp.Should().Be(value);
        }

        [Theory]
        [InlineData("internal", false)]
        [InlineData("project", true)]
        [InlineData("department", false)]
        public async Task QuerySingleAllocationRequest_WithProposedPerson_HideValue_For_Certain_Endpoints(string urlType, bool shouldHideProposedPerson)
        {
            using var adminScope = fixture.AdminScope();

            var request = await Client.CreateDefaultRequestAsync(testProject);
            var payload = new
            {
                proposedPersonAzureUniqueId = testUser.AzureUniqueId!.Value
            };
            var response = await Client.TestClientPatchAsync<JObject>($"/resources/requests/internal/{request.Id}", payload);
            response.Should().BeSuccessfull();

            var reqUrl = urlType switch
            {
                "internal" => $"/resources/requests/internal/{request.Id}",
                "project" => $"/projects/{request.Project.Id}/resources/requests/{request.Id}",
                "department" => $"/departments/{TestDepartmentId}/resources/requests/{request.Id}",
                _ => throw new BadHttpRequestException("Invalid urltype")
            };

            var internalResponse = await Client.TestClientGetAsync<TestApiInternalRequestModel>(reqUrl);
            internalResponse.Response.IsSuccessStatusCode.Should().BeTrue();

            if (shouldHideProposedPerson)
            {
                internalResponse.Value.ProposedPerson.Should().BeNull();
                internalResponse.Value.ProposedPersonAzureUniqueId.Should().BeNull();
            }
            else
            {
                internalResponse.Value.ProposedPerson.Should().NotBeNull();
                internalResponse.Value.ProposedPersonAzureUniqueId.Should().NotBeNull();

            }
        }
        [Theory]
        [InlineData("internal", false)]
        [InlineData("project", true)]
        public async Task QueryAllocationRequests_WithProposedPerson_HideValue_For_Certain_Endpoints(string urlType, bool shouldHideProposedPerson)
        {
            using var adminScope = fixture.AdminScope();

            var request = await Client.CreateDefaultRequestAsync(testProject);
            var payload = new
            {
                proposedPersonAzureUniqueId = testUser.AzureUniqueId!.Value
            };
            var response = await Client.TestClientPatchAsync<JObject>($"/resources/requests/internal/{request.Id}", payload);
            response.Should().BeSuccessfull();

            var reqUrl = urlType switch
            {
                "internal" => $"/resources/requests/internal",
                "project" => $"/projects/{request.Project.Id}/resources/requests",
                _ => throw new BadHttpRequestException("Invalid urltype")
            };

            var internalResponse = await Client.TestClientGetAsync<ApiCollection<TestApiInternalRequestModel>>(reqUrl);
            internalResponse.Response.IsSuccessStatusCode.Should().BeTrue();
            var requestInList = internalResponse.Value.Value.FirstOrDefault(x => x.Id == request.Id);
            requestInList.Should().NotBeNull();

            if (shouldHideProposedPerson)
            {
                requestInList!.ProposedPerson.Should().BeNull();
                requestInList.ProposedPersonAzureUniqueId.Should().BeNull();
            }
            else
            {
                requestInList!.ProposedPerson.Should().NotBeNull();
                requestInList.ProposedPersonAzureUniqueId.Should().NotBeNull();
            }

        }

        [Fact]
        public async Task UpdateRequest_ShouldSetProposedChanges_WhenPatchingInstanceLocation()
        {
            using var adminScope = fixture.AdminScope();

            var location = new
            {
                id = Guid.NewGuid(),
                name = "Test location"
            };

            var response = await Client.TestClientPatchAsync($"/resources/requests/internal/{normalRequest.Id}", new
            {
                proposedChanges = new { location }
            }, new
            {
                proposedChanges = new { location }
            });

            response.Should().BeSuccessfull();
            response.Value.proposedChanges.location.Should().NotBeNull();
            response.Value.proposedChanges.location.id.Should().Be(location.id);
            response.Value.proposedChanges.location.name.Should().Be(location.name);
        }

        [Fact]
        public async Task UpdateRequest_ShouldBeBadRequest_WhenPatchingInvalidDepartment()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPatchAsync<object>($"/resources/requests/internal/{normalRequest.Id}", new { assignedDepartment = "Invalid" });
            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task UpdateRequest_ShouldNotBeBadRequest_WhenPatchingDepartmentInLineOrg()
        {
            var fakeResourceOwner = fixture.AddProfile(FusionAccountType.Employee);

            var lineorgData = new
            {
                Count = 1,
                TotalCount = 1,
                Value = new[]
               {
                    new
                    {
                        fakeResourceOwner.AzureUniqueId,
                        fakeResourceOwner.Name,
                        fakeResourceOwner.Mail,
                        IsResourceOwner = true,
                        FullDepartment = "TPD LIN ORG TST"
                    }
                }
            };

            fixture.LineOrg.WithResponse("/lineorg/persons", lineorgData);

            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPatchAsync<object>($"/resources/requests/internal/{normalRequest.Id}", new { assignedDepartment = "TPD LIN ORG TST" });
            response.Should().BeSuccessfull();
        }

        [Fact]
        public async Task UpdateRequest_ShouldBadRequest_WhenPatchingInvalidProposedChanges()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPatchAsync<object>($"/resources/requests/internal/{normalRequest.Id}", new { proposedChanges = new { someRandomProp = DateTime.UtcNow } });
            response.Should().BeBadRequest();
        }

        #endregion

        #region Create request tests
        [Fact]
        public async Task CreateRequest_ShouldBeBadRequest_WhenNormalAndNoPosition()
        {
            using var adminScope = fixture.AdminScope();

            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new { }, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        [Fact]
        public void CreateRequest_ShouldSendSubscriptionEvent()
        {
            fixture.GetNotificationMessages<Integration.Models.FusionEvents.ResourceAllocationRequestSubscriptionEvent>("resources-sub")
                .Should().Contain(m => m.Payload.ItemId == normalRequest.Id && m.Payload.Type == Integration.Models.FusionEvents.EventType.RequestCreated);
        }


        [Fact]
        public async Task CreateRequest_ShouldBeBadRequest_WhenNormalAndNoInstance()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();

            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                orgPositionId = position.Id
            }, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreateRequest_ShouldBeBadRequest_WhenNormalAndInstanceNotOnPosition()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();

            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                orgPositionId = position.Id,
                orgPositionInstanceId = Guid.NewGuid()
            }, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        [Fact]
        public async Task CreateRequest_ShouldBeBadRequest_WhenNormalPositionNotExists()
        {
            using var adminScope = fixture.AdminScope();
            var position = testProject.AddPosition();

            var response = await Client.TestClientPostAsync($"/projects/{projectId}/requests", new
            {
                orgPositionId = Guid.NewGuid(),
                orgPositionInstanceId = Guid.NewGuid()
            }, new { Id = Guid.Empty });

            response.Should().BeBadRequest();
        }

        #endregion

        #region Provision

        [Fact]
        public async Task Provision_AllocationRequest_ShouldSetChangeSourceHeaders()
        {
            using var adminScope = fixture.AdminScope();

            var request = await Client.CreateDefaultRequestAsync(testProject);
            await Client.StartProjectRequestAsync(testProject, request.Id);
            await Client.ProposePersonAsync(request.Id, testUser);
            await Client.TaskOwnerApproveAsync(testProject, request.Id);

            await Client.ProvisionRequestAsync(request.Id);

            var invocations = OrgServiceMock.Invocations.Where(i => i.Headers.Any(k => k.Key == "x-fusion-change-source"));
            invocations.Should().Contain(e => e.Headers.Any(h => h.Key == "x-fusion-change-source" && h.Value.Contains($"; {request.Number}")));
        }
        #endregion
    }

}