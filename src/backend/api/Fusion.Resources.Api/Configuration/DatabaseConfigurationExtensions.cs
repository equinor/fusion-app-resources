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
using Fusion;
using Fusion.Resources.Domain;

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

            services.AddScoped<ITransactionScope, EFTransactionScope>();
            return services;
        }

        public static void SeedDatabase(this IApplicationBuilder app)
        {
            var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
                var api = scope.ServiceProvider.GetRequiredService<IOrgApiClientFactory>();
                var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

                profileService.EnsureExternalPersonnelAsync("hans.dahle@bouvet.no").Wait();
                profileService.EnsureExternalPersonnelAsync("martin.forre@bouvet.no").Wait();

                SeedPersonnel(db);

                LoadOrgChartInfoAsync(db, api).Wait();

                //SeedProjectAndContract(db);
                //SeedPersons(db);
                
                //SeedContractPersonnel(db);
            }
        }

        private static async Task LoadOrgChartInfoAsync(ResourcesDbContext db, IOrgApiClientFactory orgApiClientFactory)
        {

            var client = orgApiClientFactory.CreateClient(ApiClientMode.Application);
            var project = await client.GetProjectOrDefaultV2Async("query");
            var contracts = await client.GetContractsV2Async("query");

            var systemSeeder = new DbPerson
            {
                AzureUniqueId = Guid.Empty,
                AccountType = "Application",
                JobTitle = "System Seeder",
                Mail = "resources@fusion.equinor.com",
                Name = "Resource System Account"
            };

            var dbProject = new DbProject
            {
                DomainId = project.DomainId,
                Id = Guid.NewGuid(),
                Name = project.Name,
                OrgProjectId = project.ProjectId
            };

            var dbContracts = contracts.Select(c => new DbContract
            {
                AllocatedBy = systemSeeder,
                Allocated = DateTime.UtcNow,
                ContractNumber = c.ContractNumber,
                Name = c.Name,
                OrgContractId = c.Id,
                Project = dbProject
            });

            db.Persons.Add(systemSeeder);
            db.Projects.Add(dbProject);
            db.Contracts.AddRange(dbContracts);

            db.SaveChanges();

            var faker = new Faker();

            foreach (var contract in db.Contracts)
            {
                var persons = faker.PickRandom(db.ExternalPersonnel, faker.Random.Number(5, 10));

                SeedContractPersonnel(systemSeeder, contract, dbProject, db);
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

        public static void SeedContractPersonnel(DbPerson seeder, DbContract contract, DbProject project, ResourcesDbContext db)
        {
            var personnel = db.ExternalPersonnel.ToList();
            var persons = db.Persons.ToList();

            var faker = new Faker();
            var a = faker.PickRandom(personnel, faker.Random.Number(4, 10))
                .Select(p => new DbContractPersonnel
                {
                    ContractId = contract.Id,
                    Project = project,
                    Created = DateTimeOffset.Now,
                    CreatedBy = seeder,
                    Person = p
                }).ToList();

            if (!a.Any(p => p.Person.Mail == "hans.dahle@bouvet.no"))
                a.Add(new DbContractPersonnel
                {
                    ContractId = contract.Id,
                    Project = project,
                    Created = DateTimeOffset.Now,
                    CreatedBy = seeder,
                    Person = db.ExternalPersonnel.FirstOrDefault(p => p.Mail == "hans.dahle@bouvet.no")
                });


            db.ContractPersonnel.AddRange(a);

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
