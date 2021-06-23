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
                .FinishWith((f, p) =>
                {
                    p.Mail = f.Person.Email;
                });
        }
    }

}
