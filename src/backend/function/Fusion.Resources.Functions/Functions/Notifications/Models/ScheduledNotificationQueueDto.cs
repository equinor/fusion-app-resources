using System;

namespace Fusion.Resources.Functions.Functions.Notifications.Models;

public class ScheduledNotificationQueueDto
{
    public string AzureUniqueId { get; set; }
    public NotificationRoleType Role  { get; set; }
}
public enum NotificationRoleType
{
    ResourceOwner,
    TaskOwner
}