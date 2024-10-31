using System;

namespace Fusion.Summary.Functions.Models;

/// Message sent between weekly task owner report sync and worker
public class WeeklyTaskOwnerReportMessage
{
    public required Guid ProjectId { get; set; }
    public required Guid OrgProjectExternalId { get; set; }

    /// Mainly for debugging purposes
    public required string ProjectName { get; set; }

    public required ProjectAdmin[] ProjectAdmins { get; set; }

    public class ProjectAdmin
    {
        public required Guid AzureUniqueId { get; set; }

        public string? UPN { get; set; }

        /// Can be null due to being nullable from the roles api
        public required DateTimeOffset? ValidTo { get; set; }
    }
}