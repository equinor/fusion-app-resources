namespace Fusion.Infra.Cli.Tests
{
    /// <summary>
    /// Represents a operation posted to the mock infra api.
    /// </summary>
    public class TestInfraOperation
    {
        public ApiDatabaseRequestModel? Request { get; set; }
        public bool ProductionEnvironment { get; set; }
        public string Id { get; set; }
        public int Checks { get; set; }
    }
}