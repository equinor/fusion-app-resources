namespace Fusion.Resources.Functions.Common.Integration.ServiceDiscovery
{
    public sealed class ServiceEndpoint
    {
        public string Key { get; private set; }

        public static ServiceEndpoint People = new ServiceEndpoint { Key = "people" };
        public static ServiceEndpoint Org = new ServiceEndpoint { Key = "org" };
        public static ServiceEndpoint Resources = new ServiceEndpoint { Key = "resources" };
        public static ServiceEndpoint Summary = new ServiceEndpoint { Key = "summary" };
        public static ServiceEndpoint Notifications = new ServiceEndpoint { Key = "notifications" };
        public static ServiceEndpoint Context = new ServiceEndpoint { Key = "context" };
        public static ServiceEndpoint LineOrg = new ServiceEndpoint { Key = "lineorg" };
        public static ServiceEndpoint Roles = new ServiceEndpoint { Key = "roles" };
        public static ServiceEndpoint Mail = new ServiceEndpoint { Key = "mail" };
    }
}
