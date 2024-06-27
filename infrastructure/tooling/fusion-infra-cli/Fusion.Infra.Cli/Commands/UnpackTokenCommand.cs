using McMaster.Extensions.CommandLineUtils;

namespace Fusion.Infra.Cli.Commands
{
    [Command("unpack-token")]
    public class UnpackTokenCommand : CommandBase
    {

        [Option("-t <token>", ShortName = "t", LongName = "token", Description = "Access token to the infra api")]
        public string? AccessToken { get; set; }


        public override Task OnExecuteAsync(CommandLineApplication app)
        {
            if (AccessToken is not null)
            {
                Utils.PrintToken(AccessToken);
            }
            else
            {
                Console.WriteLine("No token provided, use -t <token>");
                
                app.ShowHelp();
            }

            return Task.CompletedTask;
        }

    }
}
