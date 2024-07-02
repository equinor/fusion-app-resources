using Azure.Core;
using Azure.Identity;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace Fusion.Infra.Cli.Commands
{
    [Command("get-token")]
    public class GetTokenCommand : CommandBase
    {

        [Required]
        [Option("-r <resource>", ShortName = "r", LongName = "resource", Description = "Get access token to a specific resource")]
        public string Resource { get; set; } = null!;


        public override async Task OnExecuteAsync(CommandLineApplication app)
        {
            var credentials = new DefaultAzureCredential();
            var token = await credentials.GetTokenAsync(new TokenRequestContext(new[] { Resource }), CancellationToken.None);

            Console.WriteLine(token.Token);
        }

    }
}
