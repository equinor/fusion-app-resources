using Fusion.Resources.Database.Entities;
using System;
using System.Collections.Generic;

namespace Fusion.Resources.Domain
{
    public class QuerySecondOpinionResult
    {
        public IReadOnlyList<QuerySecondOpinion> Value { get; }
        public QuerySecondOpinionCount Counts { get; }
        public bool IsCountOnly { get; } = false;

        public QuerySecondOpinionResult(QuerySecondOpinionCount counts)
        {
            Value = new List<QuerySecondOpinion>();
            Counts = counts;
            IsCountOnly = true;
        }

        public QuerySecondOpinionResult(List<QuerySecondOpinion> secondOpinions, QuerySecondOpinionCount counts)
        {
            Value = secondOpinions;
            Counts = counts; 
        }

        public static QuerySecondOpinionResult CreateCountOnly(List<DbSecondOpinionPrompt> secondOpinions, Guid? assigneeId)
        {
            int published = 0, total = 0;
            
            foreach (var secondOpinion in secondOpinions)
            {
                foreach (var response in secondOpinion.Responses!)
                {
                    if (response.State == DbSecondOpinionResponseStates.Published) published++;
                    total++;
                }
            }
            return new QuerySecondOpinionResult(new QuerySecondOpinionCount(total, published));
        }

        public static QuerySecondOpinionResult Create(List<DbSecondOpinionPrompt> secondOpinions, Guid? assigneeId)
        {
            int published = 0, total = 0;
            var result = new List<QuerySecondOpinion>();
            
            foreach(var secondOpinion in secondOpinions)
            {
                var filteredResponses = new List<DbSecondOpinionResponse>();
                foreach (var response in secondOpinion.Responses!)
                {
                    total++;
                    if (response.State == DbSecondOpinionResponseStates.Published)
                    {
                        published++;
                        filteredResponses.Add(response);
                    }
                    else if (response.AssignedToId == assigneeId || assigneeId == null)
                    {
                        filteredResponses.Add(response);
                    }
                }

                secondOpinion.Responses = filteredResponses;
                result.Add(new QuerySecondOpinion(secondOpinion));
            
            }
            return new QuerySecondOpinionResult(result, new QuerySecondOpinionCount(total, published));
        }
    }
}