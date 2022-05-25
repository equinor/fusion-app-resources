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
            Description = secondOpinion.Description;
            CreatedById = secondOpinion.CreatedById;
            CreatedBy = new QueryPerson(secondOpinion.CreatedBy);

            if(secondOpinion.Responses is not null)
            {
                Responses = secondOpinion.Responses.Select(x => new QuerySecondOpinionResponse(x)).ToList();
            }
        }

        public Guid Id { get; }
        public string Description { get;  }


        public Guid CreatedById { get;  }
        public QueryPerson CreatedBy { get; }

        public List<QuerySecondOpinionResponse> Responses { get; } = new();
    }
}
