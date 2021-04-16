using FluentValidation;
using Fusion.Resources.Domain.Queries;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fusion.Resources.Domain
{
    public class GetTbnPositionsTimeline : IRequest<QueryTbnPositionsTimeline>
    {
        public GetTbnPositionsTimeline(string departmentString, DateTime timelineStart, DateTime timelineEnd)
        {
            this.DepartmentString = departmentString;
            this.TimelineStart = timelineStart;
            this.TimelineEnd = timelineEnd;
        }

        public string DepartmentString { get; private set; }
        public DateTime? TimelineStart { get; set; }
        public DateTime? TimelineEnd { get; set; }

        public class Validator : AbstractValidator<GetTbnPositionsTimeline>
        {
            public Validator()
            {
                RuleFor(x => x.TimelineStart).NotNull();
                RuleFor(x => x.TimelineEnd).NotNull();
                RuleFor(x => x.TimelineEnd).GreaterThan(x => x.TimelineStart);
                RuleFor(x => x.DepartmentString).NotEmpty().WithMessage("Full department string must be provided");
            }
        }

        public class Handler : IRequestHandler<GetTbnPositionsTimeline, QueryTbnPositionsTimeline>
        {
            private readonly IMediator mediator;

            public Handler(IMediator mediator)
            {
                this.mediator = mediator;
            }

            public async Task<QueryTbnPositionsTimeline> Handle(GetTbnPositionsTimeline request, CancellationToken cancellationToken)
            {
                // Timeline date input has been verified in controller
                var filterStart = request.TimelineStart!.Value;
                var filterEnd = request.TimelineEnd!.Value;

                // Ensure utc dates
                if (filterStart.Kind != DateTimeKind.Utc)
                    filterStart = DateTime.SpecifyKind(filterStart, DateTimeKind.Utc);

                if (filterEnd.Kind != DateTimeKind.Utc)
                    filterEnd = DateTime.SpecifyKind(filterEnd, DateTimeKind.Utc);

                var tbnPositions = await mediator.Send(new GetTbnPositions(request.DepartmentString));
                var relevantPositions = tbnPositions
                    .Where(pos => 
                       (pos.AppliesTo >= filterStart && pos.AppliesTo < filterEnd)
                    || (pos.AppliesFrom > filterStart && pos.AppliesFrom <= filterEnd))
                    .ToList();

                var timeline = TimelineUtils
                    .GenerateTbnPositionsTimeline(relevantPositions, filterStart, filterEnd)
                    .ToList();

                return new QueryTbnPositionsTimeline(timeline, relevantPositions);
            }
        }
    }
}
