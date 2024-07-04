using Fusion.Infra.Cli.Mocks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Fusion.Infra.Cli.Tests
{
    public class TestCommandBuilder 
    {
        public DefaultInfraApiMessageHandler MessageHandler { get; set; } = new DefaultInfraApiMessageHandler();
        public GraphApiMessageHandler GraphMessageHandler { get; set; } = new GraphApiMessageHandler();

        public TestFileLoader FileContent { get; set; } = TestFileLoader.FromJson(Helper.GetValidBasicConfig());

        public TestAccountResolver AccountResolver { get; set; } = new TestAccountResolver();

        private Action<IServiceCollection>? customServiceSetup = null;

        public async Task<TestCommandExecutionResult> ExecuteCommand(string command)
        {
            var app = Setup.CreateCliApp(s =>
            {
                s.AddHttpClient(Constants.InfraClientName, c => { c.BaseAddress = new Uri("http://localhost"); })
                    .ConfigurePrimaryHttpMessageHandler(() => MessageHandler);
                s.AddHttpClient(Constants.GraphClientName, c => { c.BaseAddress = new Uri("http://localhost"); })
                    .ConfigurePrimaryHttpMessageHandler(c => GraphMessageHandler);
                s.AddSingleton<IFileLoader>(FileContent);
                s.AddSingleton<IAccountResolver>(AccountResolver);
                s.AddSingleton<ITokenProvider, TestTokenProvider>();

                customServiceSetup?.Invoke(s);
            });

            // Replace any quoted blocks with a single string without space.
            // Keep the original value. 
            // Now we can repalce the placeholder with the orignal value which contains space, when building the argument array.
            var values = new List<string>();
            var processedCommand = Regex.Replace(command, @"""([^""]+)""", match =>
            {
                var value = match.Groups[0].Value;
                values.Add(value.Substring(1, value.Length - 2));
                return $"__VALUE|{values.Count - 1}";
            });

            // Now we have the correct list of arguments, replace placeholders with the orignal string.
            var argList = processedCommand.Split(" ").Select(i =>
            {
                if (i.StartsWith("__VALUE"))
                {
                    return values[int.Parse(i.Split('|')[1])];
                }
                return i;
            }).ToArray();

            var result = await app.ExecuteAsync(argList);

            return new TestCommandExecutionResult
            {
                CommandResult = result,
                Operations = MessageHandler.Operations.ToList()
            };
        }

        public TestCommandBuilder WithConfigureServices(Action<IServiceCollection> setup)
        {
            this.customServiceSetup = setup;
            return this;
        }

        public void ClearOperations()
        {
            MessageHandler.Operations.Clear();
        }

        public TestCommandBuilder WithoutAutocompleteOperation()
        {
            MessageHandler.Autocomplete = false;
            return this;
        }

        public TestCommandBuilder WithFileConfig(object config)
        {
            FileContent = TestFileLoader.FromJson(config);
            return this;
        }

        public TestCommandBuilder WithAppRegistration(Action<ServicePrincipalAppReg> builder)
        {
            var appReg = new ServicePrincipalAppReg()
            {
                AppId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                DisplayName = $"Mock App reg {Guid.NewGuid()}"
            };
            builder(appReg);
            GraphMessageHandler.ServicePrincipalRegistrations.Add(appReg);
            AccountResolver.ServicePrincipals.Add(appReg);

            return this;
        }
        public TestCommandBuilder WithAppRegistration(Guid spId, string displayName, Guid? appId = null)
        {
            var appReg = new ServicePrincipalAppReg()
            {
                AppId = appId.HasValue ? appId.Value : Guid.NewGuid(),
                Id = Guid.NewGuid(),
                DisplayName = displayName ?? $"Mock App reg {Guid.NewGuid()}"
            };
            GraphMessageHandler.ServicePrincipalRegistrations.Add(appReg);
            AccountResolver.ServicePrincipals.Add(appReg);

            return this;
        }
        public TestCommandBuilder WithAppRegistration(Action<ServicePrincipalAppReg> builder, out ServicePrincipalAppReg createdAppReg)
        {
            var appReg = new ServicePrincipalAppReg()
            {
                AppId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                DisplayName = $"Mock App reg {Guid.NewGuid()}"
            };
            builder(appReg);
            GraphMessageHandler.ServicePrincipalRegistrations.Add(appReg);
            AccountResolver.ServicePrincipals.Add(appReg);

            createdAppReg = appReg;
            return this;
        }

        public struct TestCommandExecutionResult 
        {

            public List<TestInfraOperation> Operations { get; set; }
            public int CommandResult { get; set; }
        }
    }
}