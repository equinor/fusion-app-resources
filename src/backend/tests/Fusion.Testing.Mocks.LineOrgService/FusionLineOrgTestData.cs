using System;

namespace Fusion.Testing.Mocks.LineOrgService
{

    public class FusionLineOrgTestData
    {
        public static Bogus.Faker<ApiLineOrgUser> CreateTestUser()
        {
            return new Bogus.Faker<ApiLineOrgUser>()
                .RuleFor(u => u.AzureUniqueId, f => Guid.NewGuid())
                .RuleFor(u => u.Department, f => f.Commerce.Department())
                .RuleFor(u => u.JobTitle, f => f.Name.JobTitle())
                .RuleFor(u => u.Name, f => f.Person.FullName)
                .RuleFor(u => u.GivenName, f => f.Person.FirstName)
                .RuleFor(u => u.Surname, f => f.Person.LastName)
                .RuleFor(u => u.Country, f => f.Address.Country())
                .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber())
                .RuleFor(u => u.OfficeLocation, f => f.Address.City())
                .RuleFor(u => u.UserType, f => f.Phone.PhoneNumber())
                .FinishWith((f, p) => {
                    p.Mail = f.Person.Email;
                });
        }
    }

}
