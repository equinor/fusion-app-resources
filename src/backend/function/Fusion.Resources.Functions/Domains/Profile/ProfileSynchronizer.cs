using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fusion.Resources.Functions.Domains.Profile
{
    public class ProfileSynchronizer
    {
        private readonly HttpClient peopleClient;
        private readonly HttpClient resourcesClient;
        private readonly ILogger logger;

        public ProfileSynchronizer(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            peopleClient = httpClientFactory.CreateClient(HttpClientNames.Application.People);
            resourcesClient = httpClientFactory.CreateClient(HttpClientNames.Application.Resources);
            this.logger = logger;
        }

        public async Task SynchronizeAsync()
        {
            logger.LogInformation("Reading external person personnel with NoAccount or InviteSent from Resources API");

            var response = await resourcesClient.GetAsync($"personell?$filter=azureAdStatus in ('NoAccount','InviteSent')");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var personnel = JsonConvert.DeserializeAnonymousType(body, new { Value = new List<ApiPersonnel>() });
            var mailsToEnsure = personnel.Value
                .Where(p => !string.IsNullOrWhiteSpace(p.Mail))
                .Select(p => p.Mail)
                .Distinct()
                .ToList();

            logger.LogInformation($"Found {mailsToEnsure.Count} profiles to ensure");

            var peopleResponse = await peopleClient.PostAsJsonAsync($"persons/ensure?api-version=2.0", new { PersonIdentifiers = mailsToEnsure });
            peopleResponse.EnsureSuccessStatusCode();

            var peopleBody = await response.Content.ReadAsStringAsync();
            var people = JsonConvert.DeserializeObject<List<PersonValidationResult>>(peopleBody);
            var ensuredPeople = people.Where(p => p.Success).ToList();

            logger.LogInformation($"Succesfully ensured {ensuredPeople.Count} people");

            foreach (var person in ensuredPeople)
            {
                logger.LogInformation($"Processing person '{person.Identifier}'");
                var resourcesPerson = personnel.Value.FirstOrDefault(p => p.Mail == person.Identifier);

                if (!InvitationStatusMatches(person.Person.InvitationStatus, resourcesPerson.AzureAdStatus))
                {
                    logger.LogInformation($"Detected change in profile '{resourcesPerson.Mail}'. Initiating refresh.");
                    var refreshResponse = await resourcesClient.PostAsJsonAsync($"personell/{resourcesPerson.Mail}/refresh", new { });
                    refreshResponse.EnsureSuccessStatusCode();
                }
            }
        }

        private bool InvitationStatusMatches(InvitationStatus? peopleStatus, ApiAccountStatus resourcesStatus)
        {
            if ((peopleStatus == InvitationStatus.Accepted && resourcesStatus == ApiAccountStatus.Available) ||
                (peopleStatus == InvitationStatus.Pending && resourcesStatus == ApiAccountStatus.InviteSent) ||
                (peopleStatus == InvitationStatus.NotSent && resourcesStatus == ApiAccountStatus.NoAccount) ||
                (peopleStatus == null && resourcesStatus == ApiAccountStatus.NotSet))
            {
                return true;
            }

            return false;
        }

        public class PersonValidationResult
        {
            public bool Success { get; set; }
            public int StatusCode { get; set; }
            public string Message { get; set; }
            public Person Person { get; set; }
            public string Identifier { get; set; }
        }

        public class Person
        {
            public string Mail { get; set; }
            public InvitationStatus? InvitationStatus { get; set; }
        }

        public class ApiPersonnel
        {
            public Guid Id { get; set; }
            public Guid? AzureUniquePersonId { get; set; }
            public string Mail { get; set; }
            public ApiAccountStatus AzureAdStatus { get; set; }
        }

        public enum InvitationStatus { Accepted, Pending, NotSent }
        public enum ApiAccountStatus { Available, InviteSent, NoAccount, NotSet }
    }
}
