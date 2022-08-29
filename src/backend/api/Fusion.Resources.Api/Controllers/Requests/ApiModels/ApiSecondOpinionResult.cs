using Fusion.Resources.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fusion.Resources.Api.Controllers
{
    public class ApiSecondOpinionResult
    {
        public ApiSecondOpinionResult(QuerySecondOpinionResult result, Guid viewerAzureUniqueId, bool includeChildren = true )
        {
            Value = result.Value
                .Select(x => new ApiSecondOpinion(x, viewerAzureUniqueId, includeChildren))
                .ToList();
            Counts = new ApiSecondOpinionCount(result.Counts);
        }

        public List<ApiSecondOpinion> Value { get; }
        public ApiSecondOpinionCount Counts { get; }
    }
}
