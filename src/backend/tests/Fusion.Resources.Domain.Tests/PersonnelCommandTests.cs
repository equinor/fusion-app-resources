using Bogus;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Fusion.Resources.Domain.Commands;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Fusion.Resources.Domain.Tests
{
    public class PersonnelCommandTests 
    {

        private readonly DbContextOptions<ResourcesDbContext> options;
        
        private readonly DbProject testProject;
        private readonly DbContract testContract;
        private readonly DbPerson testPerson;


        public PersonnelCommandTests()
        {
            this.options = new DbContextOptionsBuilder<ResourcesDbContext>()
                .UseInMemoryDatabase($"{Guid.NewGuid()}")
                .Options;


            using (var db = new ResourcesDbContext(options))
            {
                testProject = new DbProject
                {
                    Name = $"Test project {Guid.NewGuid()}",
                    OrgProjectId = Guid.NewGuid()
                };

                testContract = new DbContract
                {
                    ContractNumber = "1122334455",
                    Name = "11 22 33 Contract 44 55",
                    OrgContractId = Guid.NewGuid(),
                    Project = testProject
                };
                testPerson = TestData.Person.Generate();

                db.Persons.Add(testPerson);
                db.Projects.Add(testProject);
                db.Contracts.Add(testContract);

                db.SaveChanges();
            }
            
        }

        ResourcesDbContext ResourcesDb => new ResourcesDbContext(options);

        //[Fact]
        //public void Create_ShouldBe_Successful()
        //{
        //    var faker = new Faker();

        //    var personId = new PersonId(faker.Person.Email);
        //    var externalPersonnel = new DbExternalPersonnelPerson { Mail = faker.Person.Email };

        //    using (var db = ResourcesDb)
        //    {
                
        //    }

        //        var profileService = new Mock<IProfileServices>();
        //    profileService.Setup(c => c.EnsurePersonAsync(testPerson.AzureUniqueId)).ReturnsAsync(testPerson);
        //    profileService.Setup(c => c.EnsureExternalPersonnelAsync(personId)).ReturnsAsync(testPerson);

        //    var command = new CreateContractPersonnel()
        //    {
        //        EditorAzureUniqueId = testPerson.AzureUniqueId,
        //        FirstName = faker.Person.FirstName,
        //        LastName = faker.Person.LastName,
        //        OrgContractId = testContract.OrgContractId,
        //        OrgProjectId = testProject.OrgProjectId,
        //        Person = new PersonId()

        //    }

        //    var handler = new CreateContractPersonnel.Handler(profileService.Object, )

        //}
    }

    public class TestData
    {
        public static Faker<DbPerson> Person => new Faker<DbPerson>()
            .RuleFor(p => p.Id, f => Guid.NewGuid())
            .RuleFor(p => p.AzureUniqueId, f => Guid.NewGuid())
            .RuleFor(p => p.Mail, f => f.Person.Email)
            .RuleFor(p => p.Name, f => f.Person.FullName)
            .RuleFor(p => p.Phone, f => f.Person.Phone);

        public static Faker<DbExternalPersonnelPerson> ExternalPersonnel => new Faker<DbExternalPersonnelPerson>()
           .RuleFor(p => p.Id, f => Guid.NewGuid())
           .RuleFor(p => p.AzureUniqueId, f => Guid.NewGuid())
           .RuleFor(p => p.UPN, f => f.Person.UserName)
           .RuleFor(p => p.Mail, f => f.Person.Email)
           .RuleFor(p => p.Name, f => f.Person.FullName)
           .RuleFor(p => p.FirstName, f => f.Person.FirstName)
           .RuleFor(p => p.LastName, f => f.Person.LastName)
            .RuleFor(p => p.AccountStatus, f => f.PickRandom<DbAzureAccountStatus>())
            .RuleFor(p => p.Disciplines, f => new List<DbPersonnelDiscipline>())
           .RuleFor(p => p.Phone, f => f.Person.Phone);
        

    }
}
