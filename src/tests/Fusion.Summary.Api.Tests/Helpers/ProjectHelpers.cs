using FluentAssertions;
using Fusion.Summary.Api.Controllers.ApiModels;
using Fusion.Testing;

namespace Fusion.Summary.Api.Tests.Helpers;

public static class ProjectHelpers
{
    public static ApiProject GenerateProject(Guid? directorAzureUniqueId = null, Guid[]? additionalAdmins = null)
    {
        var id = Guid.NewGuid();
        return new ApiProject
        {
            Id = id,
            Name = "Test Project - " + id,
            OrgProjectExternalId = Guid.NewGuid(),
            DirectorAzureUniqueId = directorAzureUniqueId,
            AssignedAdminsAzureUniqueId = additionalAdmins ?? []
        };
    }

    public static async Task<TestClientHttpResponse<ApiProject>> PutProjectAsync(this HttpClient client, Action<ApiProject>? setup = null)
    {
        var project = GenerateProject();
        setup?.Invoke(project);

        var response = await client.TestClientPutAsync<ApiProject>($"projects/{project.OrgProjectExternalId}", project);
        return response;
    }

    public static async Task<TestClientHttpResponse<ApiProject>> GetProjectAsync(this HttpClient client,
        string projectId)
    {
        var response = await client.TestClientGetAsync<ApiProject>($"projects/{projectId}");
        return response;
    }
}