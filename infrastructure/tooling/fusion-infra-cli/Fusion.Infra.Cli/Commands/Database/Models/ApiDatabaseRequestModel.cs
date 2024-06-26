using System.Text.Json.Serialization;

namespace Fusion.Infra.Cli.Commands.Database
{

    internal partial class ProvisionDatabaseCommand
    {
        public class ApiDatabaseRequestModel
        {
            public string? Name { get; set; }
            public string? Environment { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ApiAccessControl? AccessControl { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ApiSqlPermissions? SqlPermission { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public ApiPullRequestInfo? PullRequest { get; set; }

            public class ApiPullRequestInfo
            {
                public string PrNumber { get; set; } = null!;
                public string GithubRepo { get; set; } = null!;
                public string CopyFromEnvironment { get; set; } = null!;
            }


            public class ApiSqlPermissions
            {
                public List<string> Owners { get; set; } = new List<string>();
                public List<string> Contributors { get; set; } = new List<string>();
            }

            public class ApiAccessControl
            {
                public string? AdministratorGroupName { get; set; }
                public string? DeveloperGroupName { get; set; }
            }
        }

    }
}
