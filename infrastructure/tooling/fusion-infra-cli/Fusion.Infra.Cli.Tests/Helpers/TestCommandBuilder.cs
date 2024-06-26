using Microsoft.Extensions.DependencyInjection;

namespace Fusion.Infra.Cli.Tests
{
    public class TestCommandBuilder
    {
        public DefaultInfraApiMessageHandler MessageHandler { get; set; } = new DefaultInfraApiMessageHandler();

        public TestFileLoader FileContent { get; set; } = TestFileLoader.FromJson(Helper.GetValidBasicConfig());
        
        public async Task<TestCommandExecutionResult> ExecuteCommand(string command)
        {
            var app = Setup.CreateCliApp(s =>
            {
                s.AddHttpClient(Constants.InfraClientName, c => { c.BaseAddress = new Uri("http://localhost"); })
                    .ConfigurePrimaryHttpMessageHandler(() => MessageHandler);
                s.AddSingleton<IFileLoader>(FileContent);
            });

            var result = await app.ExecuteAsync(command.Split(" "));

            return new TestCommandExecutionResult
            {
                CommandResult = result,
                Operations = MessageHandler.Operations.ToList()
            };
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

        public struct TestCommandExecutionResult 
        {

            public List<TestInfraOperation> Operations { get; set; }
            public int CommandResult { get; set; }
        }
    }


    
}