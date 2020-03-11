namespace Fusion.Resources.Functions
{
    public sealed class ServiceEndpoint
    {
        public string Key { get; private set; }

        public static ServiceEndpoint People = new ServiceEndpoint { Key = "people" };
        public static ServiceEndpoint Org = new ServiceEndpoint { Key = "org" };
        public static ServiceEndpoint Resources = new ServiceEndpoint { Key = "resources" };
    }
}
