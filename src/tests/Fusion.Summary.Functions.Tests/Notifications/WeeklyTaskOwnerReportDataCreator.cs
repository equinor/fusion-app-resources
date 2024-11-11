using FluentAssertions;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Services.Org.ApiModels;
using Fusion.Summary.Functions.ReportCreator;
using Google.Protobuf.WellKnownTypes;

namespace Fusion.Summary.Functions.Tests.Notifications;

public class WeeklyTaskOwnerReportDataCreatorTests
{
    private readonly DateTime now;

    public WeeklyTaskOwnerReportDataCreatorTests()
    {
        now = DateTime.UtcNow;
        WeeklyTaskOwnerReportDataCreator.NowDate = now;
    }


    [Fact]
    public void ActiveAdmins_AreConsideredExpiring_IfValidToIsLessThanThreeMonths()
    {
        var admins = new List<PersonAdmin>()
        {
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(1))), // Is expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(50))), // Is expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(90))), // Is expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(120))), // Is not expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(365))) // Is not expiring
        };

        var data = WeeklyTaskOwnerReportDataCreator.GetExpiringAdmins(admins);

        data.Should().HaveCount(3);
    }


    [Fact]
    public void GetPositionsEndingNextThreeMonthsTest()
    {
        var nonActiveFuturePosition = new PositionBuilder()
            .WithInstance(now.AddMonths(1), now.AddMonths(2))
            .Build();
        nonActiveFuturePosition.Name = nameof(nonActiveFuturePosition);

        var nonActivePastPosition = new PositionBuilder()
            .WithInstance(now.Subtract(TimeSpan.FromDays(65)), now.Subtract(TimeSpan.FromDays(30)))
            .Build();
        nonActivePastPosition.Name = nameof(nonActivePastPosition);

        var endingPosition = new PositionBuilder()
            .WithInstance(now.Subtract(TimeSpan.FromDays(1)), now.AddMonths(2))
            .Build();
        endingPosition.Name = nameof(endingPosition);


        var nonEndingPosition = new PositionBuilder()
            .WithInstance(now.Subtract(TimeSpan.FromDays(1)), now.AddMonths(2))
            .AddNextInstance(TimeSpan.FromDays(31))
            .Build();
        nonEndingPosition.Name = nameof(nonEndingPosition);


        var data = WeeklyTaskOwnerReportDataCreator.GetPositionsEndingNextThreeMonths(new List<ApiPositionV2>
        {
            nonActiveFuturePosition,
            nonActivePastPosition,
            endingPosition,
            nonEndingPosition
        });

        data.Should().ContainSingle(p => p.Position.Id == endingPosition.Id);
    }

    [Fact]
    public void GetTBNPositionsStartingWithinThreeMonthsTests()
    {
        var person = new ApiPersonV2()
        {
            AzureUniqueId = Guid.NewGuid(),
            Name = "Test Name"
        };

        var activePositions =
            new PositionBuilder()
                .WithInstance(now.Subtract(TimeSpan.FromDays(1)), now.AddMonths(2))
                .AddNextInstance(TimeSpan.FromDays(26))
                .Build();
        activePositions.Name = nameof(activePositions);


        var nonActiveWithinThreeMonthsWithPerson =
            new PositionBuilder()
                .WithInstance(now.AddMonths(2), now.AddMonths(3), person)
                .AddNextInstance(TimeSpan.FromDays(26))
                .Build();
        nonActiveWithinThreeMonthsWithPerson.Name = nameof(nonActiveWithinThreeMonthsWithPerson);


        var nonActiveWithinThreeMonthsNoPersonButHasRequest =
            new PositionBuilder()
                .WithInstance(now.AddMonths(2), now.AddMonths(3))
                .Build();
        nonActiveWithinThreeMonthsNoPersonButHasRequest.Name = nameof(nonActiveWithinThreeMonthsNoPersonButHasRequest);


        var request = new IResourcesApiClient.ResourceAllocationRequest()
        {
            Id = Guid.NewGuid(),
            OrgPosition = new()
            {
                Id = nonActiveWithinThreeMonthsNoPersonButHasRequest.Id
            }
        };

        var nonActiveOutsideThreeMonths =
            new PositionBuilder()
                .WithInstance(now.AddMonths(4), now.AddMonths(5))
                .Build();
        nonActiveOutsideThreeMonths.Name = nameof(nonActiveOutsideThreeMonths);


        var nonActiveWithinThreeMonthsNoPerson =
            new PositionBuilder()
                .WithInstance(now.AddMonths(2), now.AddMonths(3))
                .Build();
        nonActiveWithinThreeMonthsNoPerson.Name = nameof(nonActiveWithinThreeMonthsNoPerson);


        var data = WeeklyTaskOwnerReportDataCreator.GetTBNPositionsStartingWithinThreeMonts(new List<ApiPositionV2>
        {
            activePositions,
            nonActiveWithinThreeMonthsWithPerson,
            nonActiveWithinThreeMonthsNoPerson,
            nonActiveOutsideThreeMonths
        }, [request]);

        data.Should().ContainSingle(p => p.Position.Id == nonActiveWithinThreeMonthsNoPerson.Id);
    }


    private class PositionBuilder(DateTime nowParam = default)
    {
        private readonly DateTime now = nowParam == default ? DateTime.UtcNow : nowParam;
        private readonly List<ApiPositionInstanceV2> instances = new();

        public InstanceChainBuilder WithPastInstance(ApiPersonV2? person = null, string type = "Normal")
        {
            WithInstance(now.Subtract(TimeSpan.FromDays(30)), now.Subtract(TimeSpan.FromDays(1)), person, type);
            return new InstanceChainBuilder(this);
        }

        public InstanceChainBuilder WithFutureInstance(ApiPersonV2? person = null, string type = "Normal")
        {
            WithInstance(now.Add(TimeSpan.FromDays(1)), now.Add(TimeSpan.FromDays(30)), person, type);
            return new InstanceChainBuilder(this);
        }

        public InstanceChainBuilder WithInstance(DateTime appliesFrom, DateTime appliesTo, ApiPersonV2? person = null, string type = "Normal")
        {
            instances.Add(new ApiPositionInstanceV2()
            {
                Id = Guid.NewGuid(),
                AssignedPerson = person,
                Type = type,
                AppliesFrom = appliesFrom,
                AppliesTo = appliesTo
            });
            return new InstanceChainBuilder(this);
        }

        public ApiPositionV2 Build()
        {
            var id = Guid.NewGuid();
            return new ApiPositionV2()
            {
                Id = Guid.NewGuid(),
                Name = "TestName " + id,
                ExternalId = "TestExternalId " + id,
                BasePosition = new ApiPositionBasePositionV2()
                {
                    Name = "TestBaseName " + id,
                    ProjectType = "PRD"
                },
                Instances = instances
            };
        }


        public class InstanceChainBuilder(PositionBuilder builder)
        {
            private readonly PositionBuilder builder = builder;

            public InstanceChainBuilder AddNextInstance(TimeSpan duration, ApiPersonV2? person = null, string type = "Normal")
            {
                builder.instances.Add(new ApiPositionInstanceV2()
                {
                    Id = Guid.NewGuid(),
                    AssignedPerson = person,
                    Type = type,
                    AppliesFrom = builder.instances.Last().AppliesFrom,
                    AppliesTo = builder.instances.Last().AppliesFrom.Add(duration)
                });
                return this;
            }

            public ApiPositionV2 Build()
            {
                return builder.Build();
            }
        }
    }
}