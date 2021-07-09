using Bogus;
using Fusion.Integration.Profile;
using Fusion.Integration.Profile.ApiClient;
using System;

namespace Fusion.Testing.Mocks.ProfileService
{

    public class FusionTestProfiles
    {
        public static ApiPersonProfileV3 CreateTestUser(FusionAccountType type, AccountClassification? classification = null)
        {
            return CreateTestUser()
                .RuleFor(x => x.AccountType, type)
                .RuleFor(x => x.AccountClassification, (f, p) =>
                {
                    if (classification != null)
                        return classification;
                    else
                        return type == FusionAccountType.Employee ? AccountClassification.Internal : AccountClassification.External;
                })
                .Generate();
        }

        public static Bogus.Faker<ApiPersonProfileV3> CreateTestUser()
        {
            return new Bogus.Faker<ApiPersonProfileV3>()
                .RuleFor(u => u.AzureUniqueId, f => Guid.NewGuid())
                .RuleFor(u => u.Department, f => f.Commerce.Department())
                .RuleFor(u => u.FullDepartment, f => f.Commerce.Department())
                .RuleFor(u => u.JobTitle, f => f.Name.JobTitle())
                .RuleFor(u => u.MobilePhone, f => f.Phone.PhoneNumber())
                .RuleFor(u => u.Mail, f => f.Person.Email)
                .RuleFor(u => u.Name, f => f.Person.FullName)
                .RuleFor(u => u.AccountType, f => f.PickRandom<FusionAccountType>())
                .FinishWith((f, p) =>
                {
                    p.UPN = p.Mail;
                });
        }
    }

}
