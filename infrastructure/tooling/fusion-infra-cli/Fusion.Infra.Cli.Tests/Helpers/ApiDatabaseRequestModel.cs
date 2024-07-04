namespace Fusion.Infra.Cli.Tests
{
    public class ApiDatabaseRequestModel
    {
        public string? Name { get; set; }
        public string? Environment { get; set; }

        public ApiAccessControl? AccessControl { get; set; }

        public ApiSqlPermissions? SqlPermission { get; set; }

        public ApiPullRequestInfo? PullRequest { get; set; }

        public Dictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();

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