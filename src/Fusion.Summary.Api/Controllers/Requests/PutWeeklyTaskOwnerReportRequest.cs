using FluentValidation;
using Fusion.Summary.Api.Controllers.ApiModels;

namespace Fusion.Summary.Api.Controllers.Requests;

public class PutWeeklyTaskOwnerReportRequest
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public required int ActionsAwaitingTaskOwnerAction { get; set; }
    public required ApiAdminAccessExpiring[] AdminAccessExpiringInLessThanThreeMonths { get; set; }
    public required ApiPositionAllocationEnding[] PositionAllocationsEndingInNextThreeMonths { get; set; }
    public required ApiTBNPositionStartingSoon[] TBNPositionsStartingInLessThanThreeMonths { get; set; }


    public class Validator : AbstractValidator<PutWeeklyTaskOwnerReportRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PeriodStart).NotEmpty();
            RuleFor(x => x.PeriodEnd).NotEmpty();
            RuleFor(x => x.PeriodStart).LessThan(x => x.PeriodEnd);
            RuleFor(x => x.PeriodStart).Must(x => x.DayOfWeek == DayOfWeek.Monday).WithMessage("Period start must be a Monday");
            RuleFor(x => x.PeriodEnd).Must(x => x.DayOfWeek == DayOfWeek.Monday).WithMessage("Period end must be a Monday");

            RuleFor(x => x)
                .Must(x => x.PeriodEnd.Date == x.PeriodStart.Date.AddDays(7))
                .WithMessage("Period must be exactly 7 days");
        }
    }
}