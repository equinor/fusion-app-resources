using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Bogus;
using Fusion.Resources.Database.Entities;
using Fusion.Integration.Profile;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseConfigurationExtensions
    {

        public static IServiceCollection AddResourceDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ResourcesDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            return services;
        }

        public static void SeedDatabase(this IApplicationBuilder app)
        {
            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();

                SeedProjectAndContract(db);
                SeedPersons(db);
                SeedPersonnel(db);
                SeedContractPersonnel(db);
            }
        }

        public static void SeedPersonnel(ResourcesDbContext db)
        {
            var personnel = new Faker<DbExternalPersonnelPerson>()
               .RuleFor(p => p.AzureUniqueId, f => f.PickRandom<Guid?>(new[] { (Guid?)null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }))
               .RuleFor(p => p.Name, f => f.Person.FullName)
               .RuleFor(p => p.Mail, f => f.Person.Email)
               .RuleFor(p => p.JobTitle, f => f.Name.JobTitle())
               .RuleFor(p => p.Phone, f => f.Person.Phone)
               .RuleFor(p => p.AccountStatus, f => f.PickRandomWithout<DbAzureAccountStatus>(DbAzureAccountStatus.NoAccount))
               .FinishWith((f, p) =>
               {
                   if (p.AzureUniqueId == null)
                   {
                       p.AccountStatus = DbAzureAccountStatus.NoAccount;
                   }

                   p.Disciplines = Enumerable.Range(0, f.Random.Number(1, 4)).Select(i => new DbPersonnelDiscipline { Name = f.Hacker.Adjective() }).ToList();
               })
               .Generate(new Random().Next(50, 200));

            db.AddRange(personnel);

            db.SaveChanges();
        }

        public static void SeedContractPersonnel(ResourcesDbContext db)
        {
            var contract = db.Contracts.First();
            var project = db.Projects.First();
            var personnel = db.ExternalPersonnel.ToList();
            var persons = db.Persons.ToList();

            var contractPersonnel = new Faker<DbContractPersonnel>()
                .RuleFor(x => x.ContractId, contract.Id)
                .RuleFor(x => x.Created, f => f.Date.PastOffset())
                .RuleFor(x => x.CreatedBy, f => f.PickRandom(persons))
                .RuleFor(x => x.Updated, f => f.PickRandom(new[] { (DateTimeOffset?)null, f.Date.PastOffset() }))
                .RuleFor(x => x.ProjectId, project.Id)
                .FinishWith((f, x) =>
                {
                    if (x.Updated != null)
                    {
                        x.Created = f.Date.PastOffset(1, x.Updated);
                        x.UpdatedBy = f.PickRandom(persons);
                    }
                })
                .Generate(personnel.Count);

            int i = 0;
            personnel.ForEach(p => contractPersonnel[i++].PersonId = p.Id);

            db.ContractPersonnel.AddRange(contractPersonnel);
            db.SaveChanges();
        }

        public static void SeedPersons(ResourcesDbContext db)
        {
            var persons = new Faker<DbPerson>()
                .RuleFor(p => p.AzureUniqueId, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Person.FullName)
                .RuleFor(p => p.Mail, f => f.Person.Email)
                .RuleFor(p => p.AccountType, f => $"{f.PickRandomWithout(FusionAccountType.Application)}")
                .Generate(5);

            db.Persons.AddRange(persons);
        }

        public static void SeedProjectAndContract(ResourcesDbContext db)
        {
            var projects = new Faker<DbProject>()
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.Name, f => f.Lorem.Sentence(f.Random.Int(4, 10)))
                .RuleFor(p => p.OrgProjectId, Guid.NewGuid())
                .Generate();

            db.Projects.Add(projects);

            var contract = new Faker<DbContract>()
                .RuleFor(p => p.Id, f => Guid.NewGuid())
                .RuleFor(p => p.ContractNumber, f => f.Finance.Account(10))
                .RuleFor(p => p.Name, f => f.Lorem.Sentence(f.Random.Int(4, 10)))
                .RuleFor(p => p.OrgContractId, Guid.NewGuid())
                .Generate();

            contract.Project = projects;

            db.Contracts.Add(contract);

            db.SaveChanges();
        }
    }
}
