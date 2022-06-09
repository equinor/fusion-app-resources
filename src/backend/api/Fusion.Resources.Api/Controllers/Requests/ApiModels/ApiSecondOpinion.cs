using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiSecondOpinion
    {
        public ApiSecondOpinion(QuerySecondOpinion query, Guid viewerAzureUniqueId, bool includeChildren = true)
        {
            Id = query.Id;
            Description = query.Description;
            CreatedById = query.CreatedById;
            CreatedBy = new ApiPerson(query.CreatedBy);

            if (query.Responses is not null && includeChildren)
            {
                Responses = query.Responses.Select(x => new ApiSecondOpinionResponse(x, viewerAzureUniqueId, includeParent: !includeChildren)).ToList();
            }
        }

        public Guid Id { get; }
        public string Description { get; }


        public Guid CreatedById { get; }
        public ApiPerson CreatedBy { get; }

        public List<ApiSecondOpinionResponse> Responses { get; } = new();
    }
}
