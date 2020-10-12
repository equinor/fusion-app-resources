using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Fusion.Testing.Authentication.User
{
    public enum AccountType { Employee, Consultant, External, Internal }
    public enum AuthType
    {
        Delegated,
        Application
    }

    public class TestUserBuilder
    {
        public HttpClient Client { get; private set; }

        public List<TestUserRole> Roles { get; private set; } = new List<TestUserRole>();
        public string Name { get; set; }
        public Guid AzureUniqueId { get; set; }
        public AccountType? AccountType { get; set; }
        public string Mail { get; set; }
        public AuthType AuthType { get; set; } = AuthType.Delegated;
        public Guid? AppId { get; set; }

        public TestUserBuilder(HttpClient client)
        {
            Client = client;
        }



        public HttpClient AddTestAuthToken()
        {
            TestToken testToken = null;

            switch (AuthType)
            {
                case AuthType.Delegated:
                    testToken = new TestToken
                    {
                        UniqueAzurePersonId = AzureUniqueId,
                        Name = Name,
                        UPN = Mail,
                        Roles = Roles.Select(r => r.Name).ToArray(),
                        IsAppToken = false
                    };

                    break;

                case AuthType.Application:  // Add scopes when relevant...
                    testToken = new TestToken
                    {
                        UniqueAzurePersonId = AzureUniqueId,
                        Roles = Roles.Select(r => r.Name).ToArray(),
                        IsAppToken = true,
                        AppId = AppId
                    };

                    break;
            }
            if (Client.DefaultRequestHeaders.Authorization != null)
                Client.DefaultRequestHeaders.Remove("Authorization");

            Client.DefaultRequestHeaders.Add("Authorization", AuthTokenUtilities.WrapAuthToken(testToken));

            return Client;
        }
    }
}
