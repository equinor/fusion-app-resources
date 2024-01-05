using System.Collections.Generic;
using static Fusion.Resources.Functions.ApiClients.IResourcesApiClient;
using static Fusion.Resources.Functions.Functions.Notifications.ScheduledReportContentBuilderFunction;

namespace Fusion.Resources.Functions.Functions.Notifications.Models;

public class PersonnelContent
{
    public string FullName { get; set; }
    public string? ProjectName { get; set; }
    public string? PositionName { get; set; }
    public double? TotalWorkload { get; set; }
    public PersonnelPosition? EndingPosition { get; set; }
}