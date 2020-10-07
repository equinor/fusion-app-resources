using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Fusion.Testing.Authentication.User
{
    public static class HttpClientExtensions
    {

        public static TestUserBuilder WithTestUser(this HttpClient client, Integration.Profile.ApiClient.ApiPersonProfileV3 testUser)
        {
            var builder = new TestUserBuilder(client);

            builder.AzureUniqueId = testUser.AzureUniqueId.Value;
            builder.Name = testUser.Name;
            builder.Mail = testUser.Mail;
            builder.AuthType = AuthType.Delegated;

            return builder;
        }

        public static void AddTestUserToken(this HttpRequestMessage message, Integration.Profile.ApiClient.ApiPersonProfileV3 testUser, Guid? appId)
        {
            var testToken = new TestToken
            {
                UniqueAzurePersonId = testUser.AzureUniqueId.Value,
                Name = testUser.Name,
                UPN = testUser.Mail,                
                IsAppToken = false,
                AppId = appId
            };

            message.Headers.Add("Authorization", AuthTokenUtilities.WrapAuthToken(testToken));
        }

       

    }
}
