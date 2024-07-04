using Azure.Core;
using Azure.Identity;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Infra.Cli.Commands
{
    [Command("get-token")]
    public class GetTokenCommand : CommandBase
    {
        private readonly ILogger<GetTokenCommand> logger;
        private readonly ITokenProvider tokenProvider;

        [Required]
        [Option("-r <resource>", ShortName = "r", LongName = "resource", Description = "Get access token to a specific resource")]
        public string Resource { get; set; } = null!;

        public GetTokenCommand(ILogger<GetTokenCommand> logger, ITokenProvider tokenProvider)
        {
            this.logger = logger;
            this.tokenProvider = tokenProvider;
        }

        public override async Task OnExecuteAsync(CommandLineApplication app)
        {
            try
            {
                var credentials = new DefaultAzureCredential();
                var token = await tokenProvider.GetAccessToken(Resource);

                // Not using logger, as we want a clean output here.
                Console.WriteLine(token.Token);
            } 
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not aquire token: {Message}", ex.Message);
                throw;
            }

        }

    }
}
