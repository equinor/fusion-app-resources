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
        var expiringAdmins = new List<PersonAdmin>()
        {
            new PersonAdmin(Guid.NewGuid(), "1", now.Add(TimeSpan.FromDays(1))), // Is expiring
            new PersonAdmin(Guid.NewGuid(), "2", now.Add(TimeSpan.FromDays(50))), // Is expiring
            new PersonAdmin(Guid.NewGuid(), "3", now.AddMonths(3).AddDays(-1)) // Is expiring
        };
        var nonExpiringAdmins = new List<PersonAdmin>()
        {
            new PersonAdmin(Guid.NewGuid(), "", now.AddMonths(3)), // Is not expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(120))), // Is not expiring
            new PersonAdmin(Guid.NewGuid(), "", now.Add(TimeSpan.FromDays(365))) // Is not expiring
        };

        var data = WeeklyTaskOwnerReportDataCreator.GetExpiringAdmins([..expiringAdmins, ..nonExpiringAdmins]);

        data.Should().HaveCount(expiringAdmins.Count).And.OnlyContain(admin => expiringAdmins.Select(ea => ea.FullName).Contains(admin.FullName));
    }


    // Most of the test cases taken from the image in the User story's AC
    // https://statoil-proview.visualstudio.com/Fusion%20Resource%20Allocation/_workitems/edit/43190
    [Fact]
    public void GetPositionAllocationsEndingNextThreeMonthsTest()
    {
        #region Arrange

        var testData = new ReportTestDataContainer();


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

        var activeWithFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30 * 1.5), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30 * 4), person: personA)
            .Build();
        testData.AddPosition(activeWithFutureInstance);

        var activeWithoutFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30), person: personA, extId: "1")
            .Build();
        testData.AddPosition(activeWithoutFutureInstance, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var activeWithFutureInstanceDifferentPerson = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30 * 1.5), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30 * 4), person: personB)
            .Build();
        testData.AddPosition(activeWithFutureInstanceDifferentPerson);


        var singleActiveWithoutFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.Add(TimeSpan.FromDays(30 * 1.5)), person: personA)
            .Build();
        testData.AddPosition(singleActiveWithoutFutureInstance, shouldBeIncludedInReportList: true);


        var activeWithFutureInstanceUnassignedPerson = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30 * 2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(30 * 2), person: null)
            .Build();
        testData.AddPosition(activeWithFutureInstanceUnassignedPerson, shouldBeIncludedInReportList: true, instanceSelector: i => i.AssignedPerson is null);


        var futureInstanceThatIsAlsoExpiring = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(30 * 1.5), person: personA)
            .Build();
        testData.AddPosition(futureInstanceThatIsAlsoExpiring, shouldBeIncludedInReportList: true);


        var futureInstancesThatIsAlsoExpiring = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(30 * 2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(1), person: personA, extId: "1")
            .Build();
        testData.AddPosition(futureInstancesThatIsAlsoExpiring, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var futureInstanceThatIsMissingAllocation = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(30 * 2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(1), person: null)
            .Build();
        testData.AddPosition(futureInstanceThatIsMissingAllocation, shouldBeIncludedInReportList: true, instanceSelector: i => i.AssignedPerson is null);

        var futureInstancesWhereOneIsTBN = new PositionBuilder()
            .WithInstance(now.AddDays(10), now.AddDays(30), person: personA)
            .AddNextInstance(TimeSpan.FromDays(2), person: null)
            .AddNextInstance(TimeSpan.FromDays(6), person: personA)
            .Build();
        testData.AddPosition(futureInstancesWhereOneIsTBN, shouldBeIncludedInReportList: true, instanceSelector: i => i.AssignedPerson is null);


        var futureInstanceThatIsNotExpiring = new PositionBuilder()
            .WithInstance(now.AddDays(30), now.AddDays(60), person: personA)
            .AddNextInstance(TimeSpan.FromDays(100), person: personA)
            .Build();
        testData.AddPosition(futureInstanceThatIsNotExpiring);


        // Entire gap/time-period is within the 3-month window
        var activePositionWithFutureInstanceWithSmallGap = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30), person: personA)
            .AddNextInstance(now.AddDays(30 * 2), now.AddDays(30 * 4), person: personA)
            .Build();

        testData.AddPosition(activePositionWithFutureInstanceWithSmallGap);


        var activePositionWithFutureInstanceWithLargerGap = new PositionBuilder()
            .WithInstance(Past, now.AddDays(30), person: personA, extId: "1")
            .AddNextInstance(now.AddDays(30 * 5), now.AddDays(30 * 7), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personA)
            .Build();
        testData.AddPosition(activePositionWithFutureInstanceWithLargerGap, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var manySmallInstancesWithoutFutureInstance = new PositionBuilder()
            .WithInstance(Past, now.AddDays(10), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personA)
            .AddNextInstance(TimeSpan.FromDays(10), person: personB, extId: "1")
            .Build();
        testData.AddPosition(manySmallInstancesWithoutFutureInstance, shouldBeIncludedInReportList: true, instanceSelector: i => i.ExternalId == "1");


        var nonEndingPosition = new PositionBuilder()
            .WithInstance(Past, now.AddMonths(2), person: personA)
            .AddNextInstance(TimeSpan.FromDays(31), person: personB)
            .Build();
        testData.AddPosition(nonEndingPosition);

        var nonActivePositionWithPastAllocationAndFutureTBN = new PositionBuilder()
            .WithInstance(now.AddDays(-3), now.AddDays(-2), person: personA)
            .AddNextInstance(now.AddDays(80), now.AddDays(120))
            .Build();

        testData.AddPosition(nonActivePositionWithPastAllocationAndFutureTBN);


        var testPos = new PositionBuilder()
            .WithInstance(now.AddMonths(-6), now.AddMonths(1), person: personA)
            .AddNextInstance(TimeSpan.FromDays(48), personA)
            .AddNextInstance(TimeSpan.FromDays(90), personA)
            .Build();

        testData.AddPosition(testPos);


        var shouldBeIncludedInReport = testData.ShouldBeIncludedInReport;
        var positionsToTest = testData.PositionsToTest;
        var instanceToBeIncluded = testData.InstanceToBeIncluded;

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
        data.Should().HaveSameCount(shouldBeIncludedInReport, $"Exactly these positions should be included in the report: {string.Join(", ", shouldBeIncludedInReport)}. These should not be included: {string.Join(", ", data.Select(p => p.Position.Name).Where(p => !shouldBeIncludedInReport.Contains(p)))}");
    }

    [Fact]
    public void GetTBNPositionsStartingWithinThreeMonthsTests()
    {
        var testData = new ReportTestDataContainer();

        var person = new ApiPersonV2()
        {
            AzureUniqueId = Guid.NewGuid(),
            Name = "Test Name"
        };

        var activePositionWithPerson =
            new PositionBuilder()
                .WithInstance(Past, now.AddMonths(2), person: person)
                .AddNextInstance(TimeSpan.FromDays(40), person: person)
                .Build();
        testData.AddPosition(activePositionWithPerson);


        var nonActiveWithinThreeMonthsWithPerson =
            new PositionBuilder()
                .WithInstance(now.AddMonths(2), now.AddMonths(3), person)
                .AddNextInstance(TimeSpan.FromDays(26))
                .AddNextInstance(TimeSpan.FromDays(26))
                .Build();
        testData.AddPosition(nonActiveWithinThreeMonthsWithPerson);

        var activePositionWithPersonButFutureWithoutPerson =
            new PositionBuilder()
                .WithInstance(Past, now.AddMonths(2), person: person)
                .AddNextInstance(TimeSpan.FromDays(40), extId: "1")
                .AddNextInstance(TimeSpan.FromDays(40))
                .Build();
        testData.AddPosition(activePositionWithPersonButFutureWithoutPerson, shouldBeIncludedInReportList: true, i => i.ExternalId == "1");


        var nonActiveWithinThreeMonths =
            new PositionBuilder()
                .WithInstance(now.AddMonths(2), now.AddMonths(3))
                .AddNextInstance(TimeSpan.FromDays(26), person)
                .Build();
        testData.AddPosition(nonActiveWithinThreeMonths, shouldBeIncludedInReportList: true, instanceSelector: i => i.AssignedPerson is null);

        var nonActiveWithinThreeMonthsNoPersonButHasRequest =
            new PositionBuilder()
                .WithInstance(now.AddMonths(2), now.AddMonths(3))
                .Build();
        testData.AddPosition(nonActiveWithinThreeMonthsNoPersonButHasRequest, shouldBeIncludedInReportList: false);

        var request = new IResourcesApiClient.ResourceAllocationRequest()
        {
            Id = Guid.NewGuid(),
            OrgPosition = new()
            {
                Id = nonActiveWithinThreeMonthsNoPersonButHasRequest.Id
            },
            OrgPositionInstance = new()
            {
                Id = nonActiveWithinThreeMonthsNoPersonButHasRequest.Instances.First().Id
            },
            IsDraft = false,
        };

        var nonActiveWithinThreeMonthsNoPersonButHasDraftRequest =
            new PositionBuilder()
                .WithInstance(now.AddMonths(2), now.AddMonths(3))
                .Build();
        testData.AddPosition(nonActiveWithinThreeMonthsNoPersonButHasDraftRequest, shouldBeIncludedInReportList: true);

        var draftRequest = new IResourcesApiClient.ResourceAllocationRequest()
        {
            Id = Guid.NewGuid(),
            OrgPosition = new()
            {
                Id = nonActiveWithinThreeMonthsNoPersonButHasDraftRequest.Id
            },
            OrgPositionInstance = new()
            {
                Id = nonActiveWithinThreeMonthsNoPersonButHasDraftRequest.Id
            },
            IsDraft = true
        };


        var nonActiveOutsideThreeMonths =
            new PositionBuilder()
                .WithInstance(now.AddMonths(4), now.AddMonths(5))
                .Build();
        testData.AddPosition(nonActiveOutsideThreeMonths);


        var nonActiveWithinThreeMonthsNoPerson =
            new PositionBuilder()
                .WithInstance(now.AddMonths(-3), now.AddMonths(-2))
                .AddNextInstance(now.AddMonths(2), now.AddMonths(3), extId: "1")
                .Build();
        testData.AddPosition(nonActiveWithinThreeMonthsNoPerson, shouldBeIncludedInReportList: true, i => i.ExternalId == "1");

        var pastPositionWithPerson =
            new PositionBuilder()
                .WithInstance(now.AddMonths(-3), now.AddMonths(-2), person)
                .AddNextInstance(now.AddMonths(-2), now.AddMonths(-1))
                .Build();
        testData.AddPosition(pastPositionWithPerson);


        var data = WeeklyTaskOwnerReportDataCreator.GetTBNPositionsStartingWithinThreeMonths(testData.PositionsToTest, [request, draftRequest]);

        data.Should().OnlyHaveUniqueItems();
        foreach (var positionName in testData.ShouldBeIncludedInReport)
        {
            data.Should().ContainSingle(p => p.Position.Name == positionName, $"Position {positionName} should be included in the report");
        }

        // Ensure that the starts at date is set correctly
        foreach (var (position, apiPositionInstanceV2) in testData.InstanceToBeIncluded)
        {
            data.Should().ContainSingle(p => p.StartsAt == apiPositionInstanceV2.AppliesFrom && p.Position.Id == position.Id, $"Position {position.Name} should have an instance that starts at {apiPositionInstanceV2.AppliesFrom}");
        }

        // Check that there are no extra positions that should not be included
        data.Should().HaveSameCount(testData.ShouldBeIncludedInReport,
            $"Exactly these positions should be included in the report, {string.Join(", ", testData.ShouldBeIncludedInReport)}," +
            $" these should not be included {string.Join(", ", data.Select(p => p.Position.Name).Where(p => !testData.ShouldBeIncludedInReport.Contains(p)))}");
    }


    private class ReportTestDataContainer
    {
        public List<string> ShouldBeIncludedInReport { get; } = new();
        public List<ApiPositionV2> PositionsToTest { get; } = new();
        public Dictionary<ApiPositionV2, ApiPositionInstanceV2> InstanceToBeIncluded { get; } = new();

        public void AddPosition(ApiPositionV2 position, bool shouldBeIncludedInReportList = false, Func<ApiPositionInstanceV2, bool>? instanceSelector = null, [CallerArgumentExpression("position")] string positionName = null!)
        {
            ArgumentNullException.ThrowIfNull(position);

            if (shouldBeIncludedInReportList)
                ShouldBeIncludedInReport.Add(positionName);

            PositionsToTest.Add(position);
            position.Name = positionName;

            if (shouldBeIncludedInReportList && instanceSelector is not null)
            {
                var instances = position.Instances.Where(instanceSelector).ToArray();

                if (instances.Length == 0)
                    throw new InvalidOperationException($"Test setup error: No instance found for position {positionName} that matches the selector");

                if (instances.Length > 1)
                    throw new InvalidOperationException($"Test setup error: Multiple instances found for position {positionName} that matches the selector");

                InstanceToBeIncluded.Add(position, instances.First());
            }
        }
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
                    AppliesTo = appliesTo,
                    ExternalId = extId
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