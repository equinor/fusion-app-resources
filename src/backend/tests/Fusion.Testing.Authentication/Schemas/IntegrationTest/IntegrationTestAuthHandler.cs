using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Fusion.Testing.Authentication
{

    internal class IntegrationTestAuthHandler : AuthenticationHandler<IntegrationTestAuthOptions>
    {
        private readonly IConfiguration config;
        private readonly IServiceProvider services;

        public IntegrationTestAuthHandler(IOptionsMonitor<IntegrationTestAuthOptions> options, 
            IConfiguration config,
            ILoggerFactory logger, 
            UrlEncoder encoder, 
            ISystemClock clock, 
            IServiceProvider services) 
            : base(options, logger, encoder, clock)
        {
            this.config = config;
            this.services = services;
        }

        private enum AuthType { Application, Delegated }


        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                var claims = await GatherTestUserClaimsAsync();

                var testIdentity = new ClaimsIdentity(claims, IntegrationTestAuthDefaults.AuthenticationScheme);
                var testUser = new ClaimsPrincipal(testIdentity);

                var ticket = new AuthenticationTicket(testUser, new AuthenticationProperties(), IntegrationTestAuthDefaults.AuthenticationScheme);

                // Don't think there is any scenario we want to return 401, as if headers is set, the user is requested.
                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex);
            }
        }

        private async Task<List<Claim>> GatherTestUserClaimsAsync()
        {
            var tokenDeserialized = new
            {
                UniqueAzurePersonId = Guid.Empty,
                Name = string.Empty,
                UPN = string.Empty,
                Roles = new[] { string.Empty },
                IsAppToken = false,
                AppId = (Guid?)null,
                ProjectIds = new [] { Guid.Empty }
            };

            try
            {
                var token = Request.Headers["Authorization"].ToString();
                var tokenPart = token.Split(' ')[1];
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(tokenPart));
                tokenDeserialized = JsonConvert.DeserializeAnonymousType(decoded, tokenDeserialized);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to extract test auth token from [Authorization] header. See inner exception for details.", ex);
            }

            string uniqueId = tokenDeserialized.UniqueAzurePersonId.ToString();
            var authType = tokenDeserialized.IsAppToken ? AuthType.Application : AuthType.Delegated;

            var claims = new List<Claim>
            {
                new Claim(FusionClaimTypes.AzureUniquePersonId, uniqueId)
            };

            switch (authType)
            {
                case AuthType.Delegated:
                    if (tokenDeserialized.Roles != null)
                    {
                        foreach (var role in tokenDeserialized.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }

                    if (tokenDeserialized.ProjectIds != null)
                    {
                        foreach (var projectId in tokenDeserialized.ProjectIds)
                        {
                            claims.Add(new Claim(FusionClaimTypes.FusionProjectOrgChart, $"{projectId}"));
                        }
                    }

                    var peopleClientFactory = services.GetService<IPeopleApiClientFactory>();

                    if (peopleClientFactory != null)
                    {
                        var peopleClient = peopleClientFactory.CreateApplicationClient();

                        var person = await peopleClient.GetPersonAsync(uniqueId);

                        AddNameClaims(claims, person.Name);
                        claims.Add(new Claim(ClaimTypes.Name, person.Mail));
                        claims.Add(new Claim(FusionClaimTypes.UserPrincipalName, person.UPN));
                        claims.Add(new Claim(FusionClaimTypes.Mail, person.Mail));
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(tokenDeserialized.Name))
                            throw new ArgumentException("Missing name property in token");

                        AddNameClaims(claims, tokenDeserialized.Name);
                        claims.Add(new Claim(ClaimTypes.Name, tokenDeserialized.UPN));
                        claims.Add(new Claim(FusionClaimTypes.UserPrincipalName, tokenDeserialized.UPN));
                    }

                    break;
                case AuthType.Application:
                    if (tokenDeserialized.Roles != null)
                    {
                        foreach (var role in tokenDeserialized.Roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }

                    if (tokenDeserialized.AppId.HasValue)
                        claims.Add(new Claim(FusionClaimTypes.ApplicationId, $"{tokenDeserialized.AppId.Value}"));
                    else
                    {
                        // Try look for configured 
                        var appId = config[IntgTestEnvVariables.DEFAULT_APPID];

                        if (string.IsNullOrEmpty(appId))
                            throw new InvalidOperationException($"Application test users must include an appid claim or set the env variable {IntgTestEnvVariables.DEFAULT_APPID}");

                        claims.Add(new Claim(FusionClaimTypes.ApplicationId, $"{appId}"));
                    }
                    break;
            }

            return claims;
        }

        private void AddNameClaims(List<Claim> claims, string fullName)
        {
            var tokens = fullName.Split(' ');
            if (tokens.Length > 1)
            {
                claims.Add(new Claim(ClaimTypes.Surname, tokens.Last()));

                var givenName = string.Join(" ", tokens.Take(tokens.Length - 1));
                claims.Add(new Claim(ClaimTypes.GivenName, givenName));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.GivenName, fullName));
            }

        }
    }
}
