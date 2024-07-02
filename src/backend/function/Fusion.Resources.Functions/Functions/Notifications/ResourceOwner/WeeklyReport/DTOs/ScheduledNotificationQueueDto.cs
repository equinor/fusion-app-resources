using System.Collections.Generic;

namespace Fusion.Resources.Functions.Functions.Notifications.ResourceOwner.WeeklyReport.DTOs;

public class ScheduledNotificationQueueDto
{
    public IEnumerable<string> AzureUniqueId { get; set; }
    public string FullDepartment { get; set; }
    public string DepartmentSapId { get; set; }
}