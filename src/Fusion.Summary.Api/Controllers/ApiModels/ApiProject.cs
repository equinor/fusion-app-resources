using Fusion.Summary.Api.Domain.Models;

namespace Fusion.Summary.Api.Controllers.ApiModels;

public class ApiProject
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
    public required Guid OrgProjectExternalId { get; set; }

    public Guid? DirectorAzureUniqueId { get; set; }

    public Guid[] AssignedAdminsAzureUniqueId { get; set; } = [];


    public static ApiProject FromQueryProject(QueryProject queryProject)
    {
        return new ApiProject()
        {
            Id = queryProject.Id,
            Name = queryProject.Name,
            OrgProjectExternalId = queryProject.OrgProjectExternalId,
            AssignedAdminsAzureUniqueId = queryProject.AssignedAdminsAzureUniqueId.ToArray(),
            DirectorAzureUniqueId = queryProject.DirectorAzureUniqueId
        };
    }
}