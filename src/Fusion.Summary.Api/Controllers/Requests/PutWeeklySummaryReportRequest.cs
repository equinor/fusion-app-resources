﻿using FluentValidation;
using Fusion.Summary.Api.Controllers.ApiModels;

namespace Fusion.Summary.Api.Controllers.Requests;

public class PutWeeklySummaryReportRequest
{
    public required DateTime Period { get; set; }
    public required string NumberOfPersonnel { get; set; }
    public required string CapacityInUse { get; set; }
    public required string NumberOfRequestsLastPeriod { get; set; }
    public required string NumberOfOpenRequests { get; set; }
    public required string NumberOfRequestsStartingInLessThanThreeMonths { get; set; }
    public required string NumberOfRequestsStartingInMoreThanThreeMonths { get; set; }
    public required string AverageTimeToHandleRequests { get; set; }
    public required string AllocationChangesAwaitingTaskOwnerAction { get; set; }

    public required string ProjectChangesAffectingNextThreeMonths { get; set; }

    // may be a json with the list of several users (positions) - Propertybag?
    public required ApiEndingPosition[] PositionsEnding { get; set; }

    // may be a json with the list of several users - Propertybag?
    public required ApiPersonnelMoreThan100PercentFTE[] PersonnelMoreThan100PercentFTE { get; set; }

    public class Validator : AbstractValidator<PutWeeklySummaryReportRequest>
    {
        public Validator()
        {
        }
    }
}