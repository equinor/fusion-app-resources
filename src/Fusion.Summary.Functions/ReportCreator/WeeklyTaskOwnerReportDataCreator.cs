using System;
using System.Collections.Generic;
using System.Linq;
using Fusion.Resources.Functions.Common.ApiClients;
using Fusion.Services.Org.ApiModels;

namespace Fusion.Summary.Functions.ReportCreator;

public abstract class WeeklyTaskOwnerReportDataCreator
{
    public static List<TBNPosition> GetTBNPositionsStartingWithinThreeMonts(IEnumerable<ApiPositionV2> allProjectPositions)
    {
        var nowDate = DateTimeOffset.UtcNow;
        // Exclude Products
        allProjectPositions = allProjectPositions.Where(p => p.BasePosition.ProjectType != "Product");

        var tbnPositions = new List<TBNPosition>();

        foreach (var position in allProjectPositions)
        {
            var startingInstance = position.Instances.MinBy(i => i.AppliesFrom);
            if (startingInstance.AppliesFrom.Date >= nowDate.AddMonths(3).Date)
                tbnPositions.Add(new TBNPosition(position, startingInstance.AppliesFrom));
        }

        return tbnPositions;
    }

    public static List<ExpiringPosition> GetPositionsEndingNextThreeMonths(IEnumerable<ApiPositionV2> allProjectPositions)
    {
        var nowDate = DateTimeOffset.UtcNow;

        // Exclude Products
        allProjectPositions = allProjectPositions.Where(p => p.BasePosition.ProjectType != "Product");

        var expiringPositions = new List<ExpiringPosition>();

        foreach (var position in allProjectPositions)
        {
            var endingInstance = position.Instances.MaxBy(i => i.AppliesTo);
            if (endingInstance.AppliesTo.Date <= nowDate.AddMonths(3).Date)
                expiringPositions.Add(new ExpiringPosition(position, endingInstance.AppliesTo));
        }


        return expiringPositions;
    }


    public static int GetActionsAwaitingTaskOwnerAsync(IEnumerable<IResourcesApiClient.ResourceAllocationRequest> requests)
    {
        return requests
            .Where(r => r.State is not null && !r.State.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            .Count(r => (r.HasProposedPerson && !r.State!.Equals("Created", StringComparison.OrdinalIgnoreCase) && !r.IsDraft) || r.Type == "ResourceOwnerChange");
    }

    public static List<PersonAdmin> GetExpiringAdmins(IEnumerable<PersonAdmin> admins)
    {
        var now = DateTimeOffset.UtcNow;

        return admins.Where(a => a.ValidTo <= now.AddMonths(3)).ToList();
    }
}

public record PersonAdmin(Guid AzureUniqueId, string FullName, DateTimeOffset ValidTo);

public record ExpiringPosition(ApiPositionV2 Position, DateTimeOffset ExpiresAt);

public record TBNPosition(ApiPositionV2 Position, DateTimeOffset StartsAt);