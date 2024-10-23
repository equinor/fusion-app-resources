using Fusion.Summary.Api.Database.Models;

namespace Fusion.Summary.Api.Domain.Models;

public class QueryProject
{
    public required Guid Id { get; set; }

    public required string Name { get; set; }
    public required Guid OrgProjectExternalId { get; set; }

    public Guid? DirectorAzureUniqueId { get; set; }

    public List<Guid> AssignedAdminsAzureUniqueId { get; set; } = [];

    public static QueryProject FromDbProject(DbProject dbProject)
    {
        return new QueryProject()
        {
            Id = dbProject.Id,
            Name = dbProject.Name,
            OrgProjectExternalId = dbProject.OrgProjectExternalId,
            AssignedAdminsAzureUniqueId = dbProject.AssignedAdminsAzureUniqueId.ToList(),
            DirectorAzureUniqueId = dbProject.DirectorAzureUniqueId
        };
    }

    public DbProject ToDbProject()
    {
        return new DbProject()
        {
            Id = Id,
            Name = Name,
            OrgProjectExternalId = OrgProjectExternalId,
            AssignedAdminsAzureUniqueId = AssignedAdminsAzureUniqueId.ToList()
        };
    }
}