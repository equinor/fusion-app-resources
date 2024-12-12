using System.Runtime.CompilerServices;
using FluentAssertions;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Services.Org.ApiModels;
using Fusion.Summary.Functions.ReportCreator;

namespace Fusion.Summary.Functions.Tests.Notifications;

public class WeeklyTaskOwnerReportDataCreatorTests
{
    private readonly DateTime now;

    public WeeklyTaskOwnerReportDataCreatorTests()
    {
        now = DateTime.UtcNow.Date;
        WeeklyTaskOwnerReportDataCreator.NowDate = now;
        AssertionOptions.FormattingOptions.MaxDepth = 10;
        AssertionOptions.FormattingOptions.MaxLines = 500;
    }

    private DateTime Past => now.Subtract(TimeSpan.FromDays(1));

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


    // Most of the test cases taken from the image in the User story's AC
    // https://statoil-proview.visualstudio.com/Fusion%20Resource%20Allocation/_workitems/edit/43190
    [Fact]
    public void GetPositionAllocationsEndingNextThreeMonthsTest()
    {
        #region Arrange

        var personA = new ApiPersonV2()
        {
            AzureUniqueId = Guid.NewGuid(),
            Name = "Test NameA"
        };
        var personB = new ApiPersonV2()
        {
            AzureUniqueId = Guid.NewGuid(),
            Name = "Test NameB"
        };

        var shouldBeIncludedInReport = new List<string>();
        var positionsToTest = new List<ApiPositionV2>();
        var instanceToBeIncluded = new Dictionary<ApiPositionV2, ApiPositionInstanceV2>();

        var activeWithFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30 * 1.5), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30 * 4), person: personA)
            .Build();
        AddPosition(activeWithFutureInstance);

        var activeWithoutFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30), person: personA, extId: "1")
            .Build();
        AddPosition(activeWithoutFutureInstance, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var activeWithFutureInstanceDifferentPerson = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30 * 1.5), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30 * 4), person: personB)
            .Build();
        AddPosition(activeWithFutureInstanceDifferentPerson);


        var singleActiveWithoutFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.Add(TimeSpan.FromDays(30 * 1.5)), person: personA)
            .Build();
        AddPosition(singleActiveWithoutFutureInstance, shouldBeIncludedInReportList: true);


        var activeWithFutureInstanceUnassignedPerson = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30 * 2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30 * 2), person: null)
            .Build();
        AddPosition(activeWithFutureInstanceUnassignedPerson, shouldBeIncludedInReportList: true, instanceSelector: i => i.AssignedPerson is null);


        var futureInstanceThatIsAlsoExpiring = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(30 * 2), person: personA)
            .Build();
        AddPosition(futureInstanceThatIsAlsoExpiring, shouldBeIncludedInReportList: true);


        var futureInstancesThatIsAlsoExpiring = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(30 * 2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(1), person: personA, extId: "1")
            .Build();
        AddPosition(futureInstancesThatIsAlsoExpiring, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var futureInstanceThatIsMissingAllocation = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(30 * 2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(1), person: null)
            .Build();
        AddPosition(futureInstanceThatIsMissingAllocation, shouldBeIncludedInReportList: true, instanceSelector: i => i.AssignedPerson is null);

        var futureInstancesWhereOneIsTBN = new PositionBuilder()
            .WithInstance(now.AddDays(10), now.AddDays(30), person: personA)
            .AddNextInstance(TimeSpan.FromDays(2), person: null)
            .AddNextInstance(TimeSpan.FromDays(6), person: personA)
            .Build();
        AddPosition(futureInstancesWhereOneIsTBN, shouldBeIncludedInReportList: true, instanceSelector: i => i.AssignedPerson is null);


        var futureInstanceThatIsNotExpiring = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(60), person: personA)
            .AddNextInstance(TimeSpan.FromDays(100), person: personA)
            .Build();
        AddPosition(futureInstanceThatIsNotExpiring);


        // Entire gap/time-period is within the 3-month window
        var activePositionWithFutureInstanceWithSmallGap = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30), person: personA)
            .AddNextInstance(now.AddDays(30 * 2), now.AddDays(30 * 4), person: personA)
            .Build();

        AddPosition(activePositionWithFutureInstanceWithSmallGap);


        var activePositionWithFutureInstanceWithLargerGap = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30), person: personA, extId: "1")
            .AddNextInstance(now.AddDays(30 * 5), now.AddDays(30 * 7), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personA)
            .Build();
        AddPosition(activePositionWithFutureInstanceWithLargerGap, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var manySmallInstancesWithoutFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.AddDays(10), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personB, extId: "1")
            .Build();
        AddPosition(manySmallInstancesWithoutFutureInstance, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var nonEndingPosition = new PositionBuilder()
            .WithInstance(Past, now.AddMonths(2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(31), person: personB)
            .Build();
        AddPosition(nonEndingPosition);

        var nonActivePositionWithPastAllocationAndFutureTBN = new PositionBuilder()
            .WithInstance(now.AddDays(-3), now.AddDays(-2), person: personA)
            .AddNextInstance(now.AddDays(80), now.AddDays(120))
            .Build();

        AddPosition(nonActivePositionWithPastAllocationAndFutureTBN);

        if (shouldBeIncludedInReport.Distinct().Count() != shouldBeIncludedInReport.Count)
            throw new InvalidOperationException($"Test setup error: Duplicate position names in {nameof(shouldBeIncludedInReport)}");

        if (positionsToTest.Distinct().Count() != positionsToTest.Count)
            throw new InvalidOperationException($"Test setup error: Duplicate positions in {nameof(positionsToTest)}");

        #endregion

        var data = WeeklyTaskOwnerReportDataCreator.GetPositionAllocationsEndingNextThreeMonths(positionsToTest);

        data.Should().OnlyHaveUniqueItems();
        foreach (var positionName in shouldBeIncludedInReport)
        {
            data.Should().ContainSingle(p => p.Position.Name == positionName, $"Position {positionName} should be included in the report");
        }

        // Ensure that the right expiry date is set
        foreach (var (position, apiPositionInstanceV2) in instanceToBeIncluded)
        {
            data.Should().ContainSingle(p => p.ExpiresAt == apiPositionInstanceV2.AppliesTo && p.Position.Id == position.Id, $"Position {position.Name} should have an instance that expires at {apiPositionInstanceV2.AppliesTo}");
        }

        // Check that there are no extra positions that should not be included
        data.Should().HaveSameCount(shouldBeIncludedInReport, "All positions that should be included in the report should be included");
        return;

        // Helper method
        void AddPosition(ApiPositionV2 position, bool shouldBeIncludedInReportList = false, Func<ApiPositionInstanceV2, bool>? instanceSelector = null, [CallerArgumentExpression("position")] string positionName = null!)
        {
            ArgumentNullException.ThrowIfNull(position);

            if (shouldBeIncludedInReportList)
                shouldBeIncludedInReport.Add(positionName);

            positionsToTest.Add(position);
            position.Name = positionName;

            if (shouldBeIncludedInReportList && instanceSelector is not null)
            {
                var instances = position.Instances.Where(instanceSelector).ToArray();

                if (instances.Length == 0)
                    throw new InvalidOperationException($"Test setup error: No instance found for position {positionName} that matches the selector");

                if (instances.Length > 1)
                    throw new InvalidOperationException($"Test setup error: Multiple instances found for position {positionName} that matches the selector");

                instanceToBeIncluded.Add(position, instances.First());
            }
        }
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


        var data = WeeklyTaskOwnerReportDataCreator.GetTBNPositionsStartingWithinThreeMonths(new List<ApiPositionV2>
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

        public InstanceChainBuilder WithInstance(DateTime appliesFrom, DateTime appliesTo, ApiPersonV2? person = null, string? extId = null, string type = "Normal")
        {
            instances.Add(new ApiPositionInstanceV2()
            {
                Id = Guid.NewGuid(),
                ExternalId = extId,
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

            public InstanceChainBuilder AddNextInstance(TimeSpan duration, ApiPersonV2? person = null, string? extId = null, string type = "Normal")
            {
                builder.instances.Add(new ApiPositionInstanceV2()
                {
                    Id = Guid.NewGuid(),
                    ExternalId = extId,
                    AssignedPerson = person,
                    Type = type,
                    AppliesFrom = builder.instances.Last().AppliesTo.AddDays(1),
                    AppliesTo = builder.instances.Last().AppliesTo.AddDays(1).Add(duration)
                });
                return this;
            }

            public InstanceChainBuilder AddNextInstance(DateTime appliesFrom, DateTime appliesTo, ApiPersonV2? person = null, string? extId = null, string type = "Normal")
            {
                builder.instances.Add(new ApiPositionInstanceV2()
                {
                    Id = Guid.NewGuid(),
                    AssignedPerson = person,
                    Type = type,
                    AppliesFrom = appliesFrom,
                    AppliesTo = appliesTo
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