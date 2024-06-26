using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Infra.Cli.Commands.Database
{
    [Command("database", Description = "Manage fusion database")]
    [Subcommand(
        typeof(ProvisionDatabaseCommand)
    )]
    internal class DatabaseCommand : CommandBase
    {
        public override Task OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.CompletedTask;
        }
    }
}
