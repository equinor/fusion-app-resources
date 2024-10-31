using System;
using System.Diagnostics;

namespace Fusion.Summary.Functions.Models;

/// Message sent between weekly task owner report sync and worker
[DebuggerDisplay("ProjectName: {ProjectName} - ProjectAdmins count: {ProjectAdmins.Length}")]
public class WeeklyTaskOwnerReportMessage
{
    public required Guid ProjectId { get; init; }
    public required Guid OrgProjectExternalId { get; init; }

    /// Mainly for debugging purposes
    public required string ProjectName { get; init; }

    public required ProjectAdmin[] ProjectAdmins { get; init; }

    [DebuggerDisplay("AzureUniqueId: {AzureUniqueId} - Mail: {Mail} - ValidTo: {ValidTo}")]
    public class ProjectAdmin
    {
        public required Guid AzureUniqueId { get; init; }

        public string? Mail { get; init; }

        /// Can be null due to being nullable from the roles api
        public required DateTimeOffset? ValidTo { get; init; }
    }
}