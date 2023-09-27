namespace Fusion.Resources.Functions.Functions.Notifications;

public static class ScheduledReportServiceBusSettings
{
    public const string QueueName = "queue-name";
    public const string ServiceBusConnectionString = "service-bus-connection-string";
}

public static class ScheduledReportFunctionSettings
{
    public const string ContentBuilderFunctionName = "scheduled-report-content-Builder-function";
    public const string TimerTriggerFunctionName = "scheduled-report-timer-trigger-function";
    public const string TimerTriggerFunctionSchedule = "0 0 6 * * 0";
}