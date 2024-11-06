using FluentAssertions;
using Fusion.Summary.Api.Tests.Fixture;
using Fusion.Summary.Api.Tests.Helpers;
using Fusion.Summary.Api.Tests.IntegrationTests.Base;
using Fusion.Testing;
using Xunit.Abstractions;

namespace Fusion.Summary.Api.Tests.IntegrationTests;

[Collection(TestCollections.SUMMARY)]
public class ProjectTests : TestBase
{
    private readonly SummaryApiFixture _fixture;
    private HttpClient _client;

    public ProjectTests(SummaryApiFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _client = fixture.GetClient();
        SetOutput(output);
    }

    [Fact]
    public async Task PutProject_Then_GetProject_ShouldBeSuccess()
    {
        using var adminScope = _fixture.AdminScope();
        var testUser = _fixture.Fusion.CreateUser().AsEmployee().AzureUniqueId!.Value;

        var externalId = Guid.NewGuid();

        var response = await _client.PutProjectAsync(s =>
        {
            s.OrgProjectExternalId = externalId;
            s.DirectorAzureUniqueId = testUser;
        });
        response.Should().BeSuccessfull();

        var getResponse = await _client.GetProjectAsync(externalId.ToString());
        getResponse.Should().BeSuccessfull();
        getResponse.Value!.OrgProjectExternalId.Should().Be(externalId);
    }
}