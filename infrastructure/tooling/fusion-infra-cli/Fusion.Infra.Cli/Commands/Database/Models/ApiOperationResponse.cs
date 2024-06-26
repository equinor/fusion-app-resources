namespace Fusion.Infra.Cli.Commands.Database
{

    internal partial class ProvisionDatabaseCommand
    {
        public class ApiOperationResponse
        {
            public string DatabaseName { get; set; } = null!;
            public string Id { get; set; } = null!;
            /// <summary>
            /// New, Completed, Fail
            /// </summary>
            public string Status { get; set; } = null!;
            public string Message { get; set; } = null!;
            public DateTimeOffset CreatedAt { get; set; } = default!;
            public DateTimeOffset? CompletedAt { get; set; }

            public const string STATUS_NEW = "New";
            public const string STATUS_COMPLETED = "Completed";
            public const string STATUS_FAIL = "Fail";
        }

    }
}
