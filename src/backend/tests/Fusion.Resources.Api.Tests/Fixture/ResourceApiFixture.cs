using Fusion.Resources.Database;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Fusion.Testing.Mocks.ProfileService;
using Fusion.Integration.Profile.ApiClient;
using Fusion.Testing.Mocks.ContextService;
using Fusion.Integration.Profile;
using Fusion.Testing;
using Fusion.Resources.Api.Tests.FusionMocks;
using System.Data;
using System.Linq;
using Fusion.Events;
using Newtonsoft.Json;
using Fusion.Testing.Mocks.LineOrgService;
using Fusion.Resources.Domain;
using Fusion.Services.LineOrg.ApiModels;
using Fusion.Integration.LineOrg;
using Fusion.Testing.Mocks.ProfileService.Api;
using Microsoft.EntityFrameworkCore;

namespace Fusion.Resources.Api.Tests.Fixture
{
    public class ResourceApiFixture : IDisposable
    {
        public readonly ResourcesApiWebAppFactory ApiFactory;

        public ApiFusionApplicationProfile ProViewTestApplication { get; }
        public ApiPersonProfileV3 AdminUser { get; }
        public ApiPersonProfileV3 ExternalAdminUser { get; }

        public TestClientScope ExternalAdminScope() => new TestClientScope(ExternalAdminUser);

        public TestClientScope AdminScope() => new TestClientScope(AdminUser);

        public TestClientScope UserScope(ApiPersonProfileV3 profile) => new TestClientScope(profile);

        internal void DisableMemoryCache() => ApiFactory.IsMemorycacheDisabled = true;

        public ResourceApiFixture()
        {
            ApiFactory = new ResourcesApiWebAppFactory();
            AdminUser = PeopleServiceMock.AddTestProfile()
                .WithRoles("Fusion.Resources.FullControl")
                .SaveProfile();

            ExternalAdminUser = PeopleServiceMock.AddTestProfile()
                .WithRoles("Fusion.Resources.External.FullControl")
                .SaveProfile();

            ProViewTestApplication = PeopleServiceMock.AddApplication(Guid.Parse("8d5147e1-8f73-4ad1-99b4-8efb55edf741"), "Statoil ProView Test", Guid.Empty);
        }

        public ContextResolverMock ContextResolver => ApiFactory.contextResolverMock;

        public void Dispose()
        {
            using var scope = ApiFactory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
            db.Database.EnsureDeleted();
        }

        internal ApiPersonProfileV3 AddProfile(FusionAccountType accountType)
        {
            var account = PeopleServiceMock.AddTestProfile().WithAccountType(accountType).SaveProfile();

            return account;
        }

        internal ApiPersonProfileV3 AddProfile(Action<Testing.Mocks.ProfileService.FusionTestUserBuilder> setup)
        {
            var account = PeopleServiceMock.AddTestProfile();
            setup(account);
            var profile = account.SaveProfile();
            return profile;
        }

        internal ApiPersonProfileV3 AddResourceOwner(string department)
        {
            var resourceOwner = this.AddProfile(FusionAccountType.Employee);
            resourceOwner.IsResourceOwner = true;
            resourceOwner.FullDepartment = department;
            resourceOwner.Department = new DepartmentPath(department).GetShortName();
            LineOrgServiceMock.AddTestUser()
                .MergeWithProfile(resourceOwner)
                .WithFullDepartment(department)
                .AsResourceOwner()
                .SaveProfile();

            return resourceOwner;
        }

        internal ApiOrgUnit AddOrgUnit(string sapId, string name, string fullDepartment)
        {
            return LineOrgServiceMock.AddOrgUnit(sapId, name, fullDepartment, fullDepartment, fullDepartment);

        }
        internal ApiOrgUnit AddOrgUnit(string fullDepartment)
        {
            return LineOrgServiceMock.AddOrgUnit(fullDepartment);

        }

        internal DbScope DbScope() => new DbScope(ApiFactory.Services);

        internal void EnsureDepartment(string departmentId, string sectorId = null, ApiPersonProfileV3 defactoResponsible = null, int daysFrom = -1, int daysTo = 1)
        {
            LineOrgServiceMock.AddDepartment(departmentId);

            if (defactoResponsible is not null)
            {
                using (var dbScope = new DbScope(ApiFactory.Services))
                {
                    dbScope.DbContext.DelegatedDepartmentResponsibles.Add(new Database.Entities.DbDelegatedDepartmentResponsible()
                    {
                        DateFrom = DateTime.Today.AddDays(daysFrom),
                        DateTo = DateTime.Today.AddDays(daysTo),
                        DepartmentId = departmentId,
                        ResponsibleAzureObjectId = defactoResponsible.AzureUniqueId.GetValueOrDefault(),
                        Reason = "Just for testing"
                    });
                    try { dbScope.DbContext.SaveChanges(); } catch (DBConcurrencyException) { }
                }

                RolesClientMock.AddPersonRole(defactoResponsible.AzureUniqueId!.Value, new Fusion.Integration.Roles.RoleAssignment
                {
                    Identifier = $"{Guid.NewGuid()}",
                    RoleName = "Fusion.Resources.ResourceOwner",
                    Scope = new Fusion.Integration.Roles.RoleAssignment.RoleScope("OrgUnit", departmentId),
                    ValidTo = DateTime.Today.AddDays(daysTo),
                    Source = "Department.Test"
                }).GetAwaiter().GetResult();
            }
        }


        public IReadOnlyCollection<CloudEventV1<TPayload>> GetNotificationMessages<TPayload>(string pathFilter)
        {
            var messages = TestMessageBus.GetAllMessages().Where(m => m.Path == pathFilter);

            var notifications = messages.Select(m =>
            {
                try
                {
                    return JsonConvert.DeserializeObject<CloudEventV1<TPayload>>(m.BodyText);
                }
                catch (Exception) { return null; }
            }).Where(m => m != null);

            return notifications.ToList();
        }
    }

    public class DbScope : IDisposable
    {
        private IServiceScope scope;
        public ResourcesDbContext DbContext { get; }

        public DbScope(IServiceProvider apiServices)
        {
            scope = apiServices.CreateScope();
            DbContext = scope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
        }

        public void Dispose()
        {
            scope.Dispose();
        }
    }
}