using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Domain
{
    public class QuerySecondOpinion
    {
        public QuerySecondOpinion(DbSecondOpinionPrompt secondOpinion)
        {
            Id = secondOpinion.Id;
            Number = secondOpinion.Number;
            Description = secondOpinion.Description;
            Title = secondOpinion.Title;
            CreatedById = secondOpinion.CreatedById;
            CreatedBy = new QueryPerson(secondOpinion.CreatedBy);
            CreatedDate = secondOpinion.CreatedDate;

            if(secondOpinion.Responses is not null)
            {
                Responses = secondOpinion.Responses.Select(x => new QuerySecondOpinionResponse(x));
            }

            if(secondOpinion.Request is not null)
            {
                Request = new QueryResourceAllocationRequest(secondOpinion.Request);
            }
        }

        public Guid Id { get; }
        public int Number { get; }
        public string Title { get; }
        public string Description { get;  }

        public Guid CreatedById { get;  }
        public QueryPerson CreatedBy { get; }
        public DateTimeOffset CreatedDate { get; }
        public IEnumerable<QuerySecondOpinionResponse> Responses { get; } = Array.Empty<QuerySecondOpinionResponse>();
        public QueryResourceAllocationRequest? Request { get; }
    }
}
