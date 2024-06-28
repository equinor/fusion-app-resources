using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Fusion.Infra.Cli.Commands.Database
{
    [Command("provision", Description = "Provision database")]
    internal partial class ProvisionDatabaseCommand : CommandBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IFileLoader fileLoader;
        private readonly ITokenProvider tokenProvider;
        private readonly IAccountResolver accountResolver;
        public const int DefaultOperationWaitTimeout = 300;

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

        [Option("--timeout <seconds>", Description = "Set custom timeout in seconds, for the wait. Default is 300 seconds.")]
        public int? TimeoutInSeconds { get; set; }

        [Option("--sql-owner <objectId>", CommandOptionType.MultipleValue, Description = "Define owners, multiples can be defined with --sql-owner GUID --sql-owner <string>")]
        public List<string>? SqlOwners { get; set; }

        [Option("--sql-contributor <objectId>", CommandOptionType.MultipleValue, Description = "Define contributors, given data reader/writer role. Multiples can be defined with --sql-contributor GUID --sql-contributor <string>")]
        public List<string>? SqlContributor { get; set; }

        [Option("--sql-contributor-client-id <appClientId>", CommandOptionType.MultipleValue, Description = "Define contributors, using the azure ad app registration client id")]
        public List<string>? SqlContributorClientId { get; set; }

        [Option("--sql-owner-client-id <appClientId>", CommandOptionType.MultipleValue, Description = "Define owners, using the azure ad app registration client id")]
        public List<string>? SqlOwnerClientId { get; set; }

        [Option("--ignore-resolve-errors", CommandOptionType.NoValue, Description = "Command will not fail if elements are not resolved. It will log and continue.")]
        public bool IgnoreResolveErrors { get; set; }

        [Option("-o <filePath>", ShortName = "o", LongName = "output", Description = "Dump final respons from the provisioning to a specific file")]
        public string? OutputFile { get; set; }

        public ProvisionDatabaseCommand(IHttpClientFactory httpClientFactory, IFileLoader fileLoader, ITokenProvider tokenProvider, IAccountResolver accountResolver)
        {
            this.httpClientFactory = httpClientFactory;
            this.fileLoader = fileLoader;
            this.tokenProvider = tokenProvider;
            this.accountResolver = accountResolver;
        }

        public override async Task OnExecuteAsync(CommandLineApplication app)
        {
            //app.ShowHelp();
            var client = httpClientFactory.CreateClient(Constants.InfraClientName);
            if (!string.IsNullOrEmpty(InfraUrl))
                client.BaseAddress = new Uri(InfraUrl);

            await LoadAccessTokenAsync(client);

            var config = LoadConfigFile();

            LoadArgumentOverrides(config);

            await LoadSqlPermissionsAsync(config);

            var location = await StartOperationAsync(client, config);

            if (NoWait)
            {
                Console.WriteLine("Operation started @ " + location);
                return;
            }

            var provisionResponse = await WaitForOperationAsync(client, location);
            if (OutputFile != null)
            {
                File.WriteAllText(OutputFile, provisionResponse);
            }
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

        private async Task LoadAccessTokenAsync(HttpClient client)
        {
            if (string.IsNullOrEmpty(AccessToken))
            {
                Console.WriteLine("No access token found, trying to get access token using default credentials... Use [-t <token>] to specify token.");
                var token = await tokenProvider.GetAccessToken(Constants.RESOURCE_INFRA_API);
                AccessToken = token.Token;
            }

            if (!string.IsNullOrEmpty(AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", AccessToken);
                Utils.AnalyseToken(AccessToken);
            }
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

            if (SqlOwners?.Count > 0)
            {
                if (config.SqlPermission is null)
                    config.SqlPermission = new ApiDatabaseRequestModel.ApiSqlPermissions();

                config.SqlPermission.Owners.AddRange(SqlOwners);
            }

            if (SqlContributor?.Count > 0)
            {
                if (config.SqlPermission is null)
                    config.SqlPermission = new ApiDatabaseRequestModel.ApiSqlPermissions();

                config.SqlPermission.Contributors.AddRange(SqlContributor);
            }
        }

        private async Task LoadSqlPermissionsAsync(ApiDatabaseRequestModel config)
        {
            await ResolveClientIdsSqlPermissionsAsync(config);

            // Only process if this is defined.
            if (config.SqlPermission is null)
                return;

            config.SqlPermission.Contributors = await ResolveServicePrincipalsAsync(config.SqlPermission.Contributors);
            config.SqlPermission.Owners = await ResolveServicePrincipalsAsync(config.SqlPermission.Owners);

        }

        private async Task<List<string>> ResolveServicePrincipalsAsync(List<string> items)
        {
            
            var processedList = new List<string>();

            foreach (var item in items)
            {
                if (Guid.TryParse(item, out _))
                    processedList.Add(item);
                else
                {
                    if (string.Equals(item, "[currentUser]"))
                    {
                        var userId = Utils.GetCurrentUserFromToken(AccessToken ?? "");
                        if (!IgnoreResolveErrors && string.IsNullOrEmpty(userId))
                            throw new ArgumentException("Could not resolve current user token using access token");

                        if (!string.IsNullOrEmpty(userId))
                            processedList.Add(userId);
                    }

                    // Not guid, must resolve to guid.
                    var servicePrincipal = await accountResolver.ResolveAccountAsync(item, IgnoreResolveErrors);

                    if (servicePrincipal.HasValue)
                    {
                        Console.WriteLine($"> Resolved sp '{item}' to the object id '{servicePrincipal}'");
                        processedList.Add($"{servicePrincipal}");
                    }
                    else
                    {
                        Console.WriteLine($"# WARN: Could not resolve service principal using '{item}' as display name");
                    }
                }
            }

            return processedList;
        }
        
        private async Task ResolveClientIdsSqlPermissionsAsync(ApiDatabaseRequestModel config)
        {
            if (SqlContributorClientId?.Count > 0 || SqlOwnerClientId?.Count > 0)
            {
                if (config.SqlPermission is null)
                    config.SqlPermission = new ApiDatabaseRequestModel.ApiSqlPermissions();
            }

            if (SqlContributorClientId is not null)
                await ResolveAndAppendClientIdsAsync(SqlContributorClientId, config.SqlPermission!.Contributors);
            if (SqlOwnerClientId is not null)
                await ResolveAndAppendClientIdsAsync(SqlOwnerClientId, config.SqlPermission!.Owners);
        }

        private async Task ResolveAndAppendClientIdsAsync(List<string>? clientIds, List<string> appendTo)
        {
            if (clientIds is null)
                return;

            foreach (var clientId in clientIds)
            {
                var spId = await accountResolver.ResolveAppRegServicePrincipalAsync(clientId);
                if (spId.HasValue)
                {
                    Console.WriteLine($"> Resolved client id '{clientId}' to the object id '{spId}'");
                    appendTo.Add($"{spId}");
                }
                else
                {
                    Console.WriteLine($"# WARN: Could not resolve service principal using for app id '{clientId}'");
                }
            }
        }

        private async Task<string> StartOperationAsync(HttpClient client, ApiDatabaseRequestModel config)
        {
            var url = SqlProductionEnvironment ? "/sql-servers/production/databases?api-version=1.0" : "/sql-servers/non-production/databases?api-version=1.0";

            Console.WriteLine($"Triggering provisioning to [{url}]");
            Console.WriteLine("-- Payload --");
            Console.WriteLine(JsonSerializer.Serialize(config, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));
            Console.WriteLine("-- / --");

            var resp = await client.PostAsJsonAsync(url, config);

            var respData = await EnsureSuccessfullResponseAsync(resp);

            Console.WriteLine($"-- resp: {respData}");

            var locationHeader = resp.Headers.Location;

            Console.WriteLine($"Operation location: {locationHeader}");

            if (locationHeader is null)
            {
                throw new InvalidOperationException("Did not locate any operation link in location header. Cannot poll for result");
            }

            return locationHeader.ToString();
        }

        private async Task<string> WaitForOperationAsync(HttpClient client, string location)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutInSeconds.GetValueOrDefault(DefaultOperationWaitTimeout)));

            Console.CancelKeyPress += delegate {
                timeout.Cancel();
            };


        CheckOperation:
            timeout.Token.ThrowIfCancellationRequested();

            var resp = await client.GetAsync(location, timeout.Token);
            Console.WriteLine($"{resp.RequestMessage?.Method} {resp.RequestMessage?.RequestUri} → [{resp.StatusCode}]");
            var content = await EnsureSuccessfullResponseAsync(resp);
            Console.WriteLine($"-- resp: {content}");

            var responsData = JsonSerializer.Deserialize<ApiOperationResponse>(content, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (responsData?.GetStatus() == ApiOperationStatus.Pending)
            {
                await Task.Delay(TimeSpan.FromSeconds(RetryIn.GetValueOrDefault(20)), timeout.Token);
                goto CheckOperation;
            }
            
            if (responsData is not null)
                Console.WriteLine(JsonSerializer.Serialize<ApiOperationResponse>(responsData, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }));

            return content;
        }

        private async Task<string> EnsureSuccessfullResponseAsync(HttpResponseMessage resp) 
        {
            var respData = await resp.Content.ReadAsStringAsync();

            Console.WriteLine($"{resp.RequestMessage?.Method} {resp.RequestMessage?.RequestUri} → [{resp.StatusCode}]");
            
            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine("-- Response payload --");
                Console.WriteLine(respData);
                Console.WriteLine("-- / --");

                resp.EnsureSuccessStatusCode();
            }

            return respData;
        }

    }
}
