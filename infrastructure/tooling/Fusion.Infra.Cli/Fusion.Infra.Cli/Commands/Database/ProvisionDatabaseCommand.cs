using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;

namespace Fusion.Infra.Cli.Commands.Database
{
    [Command("provision", Description = "Provision database")]
    //[Subcommand()]
    internal partial class ProvisionDatabaseCommand : CommandBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IFileLoader fileLoader;

        [Required]
        [Option("-u <infraUrl>", ShortName = "u", LongName = "url", Description = "Url to fusion infra support api")]
        public string InfraUrl { get; set; } = null!;

        [Option("--production", Description = "Target the production sql environment. Defaults to non-production server.")]
        public bool SqlProductionEnvironment { get; set; }

        [Option("-t <token>", ShortName = "t", LongName = "token", Description = "Access token to the infra api")]
        public string? AccessToken { get; set; }

        [Option("-f <file>", LongName = "file", Description = "File with config json for database provisioning")]
        public string? ConfigPath { get; set; }

        [Option("-e <appEnvironment>", LongName = "env", Description = "Override environment")]
        public string? AppEnvironment { get; set; }

        [Option("-pr <pullRequestNumber>", LongName = "pull-request", Description = "Connect the database to a pull request")]
        public string? PullRequest { get; set; }

        [Option("-ghr <githubRepo>", LongName = "repo", Description = "Github repo name, required when using the pr option")]
        public string? GithubRepo { get; set; }

        [Option("-c <copyFromEnv>", LongName = "copy-from", Description = "Copy database from specified environment, when pull request is enabled")]
        public string? CopyFrom { get; set; }

        [Option("-v", LongName = "verbose", Description = "Verbose logging output.")]
        public bool VerboseLogging { get; set; } = false;

        [Option("--no-wait", Description = "By default the operation will wait for async provision operation to complete. Use no-wait to ignore waiting.")]
        public bool NoWait { get; set; } = false;

        [Option("-rt <retryInterval>", LongName = "retry-in", Description = "Set custom retry interval")]
        public int? RetryIn { get; set; }

        public ProvisionDatabaseCommand(IHttpClientFactory httpClientFactory, IFileLoader fileLoader)
        {
            this.httpClientFactory = httpClientFactory;
            this.fileLoader = fileLoader;
        }

        public override async Task OnExecuteAsync(CommandLineApplication app)
        {
            //app.ShowHelp();
            var client = httpClientFactory.CreateClient(Constants.InfraClientName);

            var config = LoadConfigFile();

            LoadArgumentOverrides(config);
           

            var location = await StartOperationAsync(client, config);

            if (NoWait)
            {
                Console.WriteLine("Operation started @ " + location);
                return;
            }

            await WaitForOperationAsync(client, location);
        }

        private ApiDatabaseRequestModel LoadConfigFile()
        {
            if (string.IsNullOrEmpty(ConfigPath))
            {
                throw new ArgumentNullException("config file path must be specified");
            }
            if (!fileLoader.Exists(ConfigPath))
            {
                throw new FileNotFoundException(ConfigPath);
            }

            // Load file
            var configFileData = fileLoader.GetContent(ConfigPath);

            var config = JsonSerializer.Deserialize<ApiDatabaseRequestModel>(configFileData, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (config == null)
                throw new ArgumentException("Could not deserialize config from file");

            return config;
        }

        private void LoadArgumentOverrides(ApiDatabaseRequestModel config)
        {
            if (!string.IsNullOrEmpty(AppEnvironment))
            {
                config.Environment = AppEnvironment;
            }

            if (!string.IsNullOrEmpty(PullRequest))
            {
                if (config.PullRequest is null)
                {
                    config.PullRequest = new ApiDatabaseRequestModel.ApiPullRequestInfo();
                }

                config.PullRequest.PrNumber = PullRequest;

                // Set env to pr if not provided
                if (string.IsNullOrEmpty(AppEnvironment))
                {
                    config.Environment = "pr";
                }
            }

            if (!string.IsNullOrEmpty(GithubRepo))
            {
                if (config.PullRequest is null)
                {
                    config.PullRequest = new ApiDatabaseRequestModel.ApiPullRequestInfo();
                }

                config.PullRequest.GithubRepo = GithubRepo;
            }

            if (!string.IsNullOrEmpty(CopyFrom))
            {
                if (config.PullRequest is null)
                {
                    config.PullRequest = new ApiDatabaseRequestModel.ApiPullRequestInfo();
                }

                config.PullRequest.CopyFromEnvironment = CopyFrom;
            }
        }

        private async Task<string> StartOperationAsync(HttpClient client, ApiDatabaseRequestModel config)
        {
            var url = SqlProductionEnvironment ? "/sql-servers/production/databases?api-version=1.0" : "/sql-servers/non-production/databases?api-version=1.0";
            var resp = await client.PostAsJsonAsync(url, config);

            var respData = await EnsureSuccessfullResponseAsync(resp);

            var locationHeader = resp.Headers.Location;

            if (locationHeader is null)
            {
                throw new InvalidOperationException("Did not locate any operation link in location header. Cannot poll for result");
            }

            return locationHeader.ToString();
        }

        private async Task WaitForOperationAsync(HttpClient client, string location)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(300));

            Console.CancelKeyPress += delegate {
                timeout.Cancel();
            };


        CheckOperation:
            timeout.Token.ThrowIfCancellationRequested();

            var resp = await client.GetAsync(location, timeout.Token);
            var content = await EnsureSuccessfullResponseAsync(resp);
            var responsData = JsonSerializer.Deserialize<ApiOperationResponse>(content);


            if (resp.StatusCode == System.Net.HttpStatusCode.Accepted 
                || string.Equals(responsData?.Status, ApiOperationResponse.STATUS_NEW, StringComparison.OrdinalIgnoreCase))
            {
                if (VerboseLogging)
                {
                    Console.WriteLine("Waiting for provisioning...");
                }

                await Task.Delay(RetryIn.GetValueOrDefault(20), timeout.Token);
                goto CheckOperation;
            }

            if (responsData is not null)
                Console.WriteLine(JsonSerializer.Serialize<ApiOperationResponse>(responsData, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
        }

        private async Task<string> EnsureSuccessfullResponseAsync(HttpResponseMessage resp) 
        {
            var respData = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed request: {resp.RequestMessage?.Method} {resp.RequestMessage?.RequestUri}");
                Console.WriteLine(respData);

                resp.EnsureSuccessStatusCode();
            }

            return respData;
        }

    }
}
