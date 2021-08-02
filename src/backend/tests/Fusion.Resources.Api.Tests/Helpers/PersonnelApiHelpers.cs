using Bogus;
using Fusion.Testing;
using Fusion.Testing.Mocks.OrgService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Fusion.Resources.Api.Tests
{
    public static partial class PersonnelApiHelpers
    {
        public static async Task<TestApiPersonnel> CreatePersonnelAsync(this HttpClient client, Guid projectId, Guid contractId, Action<TestCreatePersonnelRequest> setup = null)
        {
            var person = new Faker<TestCreatePersonnelRequest>()
                .RuleFor(p => p.Mail, f => f.Person.Email)
                .RuleFor(p => p.FirstName, f => f.Person.FirstName)
                .RuleFor(p => p.LastName, f => f.Person.LastName)
                .RuleFor(p => p.PhoneNumber, f => f.Person.Phone)
                .Generate();

            setup?.Invoke(person);
            
            var createResp = await client.TestClientPostAsync<TestApiPersonnel>($"/projects/{projectId}/contracts/{contractId}/resources/personnel", person);
            createResp.Should().BeSuccessfull();

            return createResp.Value;
        }



        public class TestCreatePersonnelRequest
        {
            public string Mail { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string PhoneNumber { get; set; }
        }
    }
}
