using System;

namespace Fusion.Testing.Authentication.User
{
    public class TestToken
    {
        public Guid UniqueAzurePersonId { get; set; }
        public string Name { get; set; }
        public string UPN { get; set; }
        public string[] Roles { get; set; }
        public bool IsAppToken { get; set; }
        public Guid? AppId { get; set; }
        public Guid[] ProjectIds { get; set; }
    }
}
