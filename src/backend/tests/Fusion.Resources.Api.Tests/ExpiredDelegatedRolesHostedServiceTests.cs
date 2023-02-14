using FluentAssertions;
using Fusion.Resources.Api.Tests.Fixture;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Fusion.Resources.Database;
using Fusion.Resources.Database.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using Fusion.Resources.Api.HostedServices;

namespace Fusion.Resources.Domain.Tests
{
    public class ExpiredDelegatedRolesHostedServiceTests : IClassFixture<ResourceApiFixture>
    {
        private ResourceApiFixture fixture;

        public ExpiredDelegatedRolesHostedServiceTests(ResourceApiFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            fixture.ApiFactory.IsMemorycacheDisabled = true;

        }
        [Fact]
        public async Task ExpiredDelegatedDepartmentResponsible_Should_BeDeleted_WhenBackgroundJobRuns()
        {
            #region Arrange

            var services = new ServiceCollection();
            services.AddDbContext<ResourcesDbContext>(opt =>
            {
                opt.UseInMemoryDatabase("HostedServicesTests");
                opt.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
            services.AddSingleton(s => new TelemetryClient(TelemetryConfiguration.CreateDefault()));

            var sp = services.BuildServiceProvider();

            #region setup test data
            var testExpiredRole = new DbDelegatedDepartmentResponsible
            {
                Id = Guid.NewGuid(),
                DepartmentId = "TEST DEP ART MENT",
                DateCreated = DateTime.UtcNow,
                Reason = $"{Guid.NewGuid()}",
                ResponsibleAzureObjectId = Guid.NewGuid(),
                DateFrom = DateTime.UtcNow.AddMinutes(-10),
                DateTo = DateTime.UtcNow.AddMinutes(-1)
            };

            using (var dbScope = sp.CreateScope())
            {
                var db = dbScope.ServiceProvider.GetRequiredService<ResourcesDbContext>();
                db.DelegatedDepartmentResponsibles.Add(testExpiredRole);
                await db.SaveChangesAsync();
            }
            #endregion

            #endregion

            #region Act
            using (var testScope = sp.CreateScope())
            {
                var scopeFactory = testScope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
                var bgService = new ExpiredDelegatedRolesHostedService(scopeFactory);

                await bgService.ExecuteTimerAsync();
            }
            #endregion

            #region Assert

            // Verify role was moved
            using (var verificationScope = sp.CreateScope())
            {
                var db = verificationScope.ServiceProvider.GetRequiredService<ResourcesDbContext>();

                db.DelegatedDepartmentResponsibles.Should().BeEmpty();
                db.DelegatedDepartmentResponsiblesHistory.Should().NotBeEmpty();
            }


            #endregion

        }

    }
}
